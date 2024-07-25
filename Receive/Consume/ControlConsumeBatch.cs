using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Receive.Model;
using System;
using System.Collections.Concurrent;
using System.Text;
using System.Threading.Tasks;

namespace Receive.Consume
{
    /// <summary>
    /// 控制消费 - 一个消费者并行处理多条消息，共用一个链接
    /// </summary>
    public class ControlConsumeBatch
    {
        private readonly IConnection _connection;
        private readonly IModel _channel;
        private readonly ushort _batchCount;

        private readonly string _exChangeName;
        private readonly string _queueName;
        private readonly string _routingKeyName;

        private readonly Action<string> _action;

        private readonly ConcurrentStack<string> _customerTags;


        public ControlConsumeBatch(ushort batchCount, string exChangeName, string queueName, string routingKeyName, Action<string> action)
        {
            _exChangeName = exChangeName;
            _queueName = queueName;
            _routingKeyName = routingKeyName;
            _action = action;
            _batchCount = batchCount;

            _customerTags = new ConcurrentStack<string>();


            _connection = MQInitForReceivedConsume.GetConnectionBatch(_batchCount);
            _channel = _connection.CreateModel();

            // 指示通道不预取超过batchCount个消息
            _channel.BasicQos(0, batchCount, false);
            // 创建一个新的，持久的交换区
            _channel.ExchangeDeclare(_exChangeName, ExchangeType.Direct, true, false, null);
            // 创建一个新的，持久的队列
            _channel.QueueDeclare(_queueName, true, false, false, null);
            // 绑定队列到交换区
            _channel.QueueBind(_queueName, _exChangeName, _routingKeyName);
        }

        /// <summary>
        /// 多条并行消费 - 创建消费者
        /// </summary>
        /// <param name="count">消费者数量</param>
        public void CreateConsumer(int count)
        {
            for (int i = 0; i < count; i++)
            {
                // 定义这个队列的消费者
                var consumer = new AsyncEventingBasicConsumer(_channel);

                consumer.Received += async (model, ea) =>
                {
                    await Task.Run(() =>
                    {
                        var body = ea.Body.ToArray();
                        var message = Encoding.UTF8.GetString(body);

                        _action(message);

                        // 手动应答消息
                        lock (_channel)
                        {
                            _channel.BasicAck(ea.DeliveryTag, false);
                        }
                    });
                };

                lock (_channel)
                {
                    var tag = Guid.NewGuid().ToString("N");
                    _customerTags.Push(tag);
                    _channel.BasicConsume(queue: _queueName, autoAck: false, consumer: consumer, consumerTag: tag);
                }
            }
        }

        /// <summary>
        /// 多条并行消费 - 移除消费者
        /// </summary>
        /// <param name="count">移除消费者数量</param>
        public void RemoveConsumer(int count)
        {
            for (int i = 0; i < count; i++)
            {
                if (_customerTags.TryPop(out string tag))
                {
                    _channel.BasicCancel(tag);
                }
            }
        }


        /// <summary>
        /// 调整消费者数量，每个消费者一次并行处理多条消息
        /// </summary>
        /// <param name="perConsumerMsgCount">每个消费者对应多少个消息数量</param>
        /// <param name="maxConsumeCount">最大消费者数量，默认0，代表消费者数量无上限</param>
        /// <param name="minConsumeCount">最小消费者数量，默认0，代表在没有消息时没有消费者</param>
        public async void AdjustConsumeCount(uint perConsumerMsgCount, uint maxConsumeCount = 0, uint minConsumeCount = 0)
        {
            if (minConsumeCount > maxConsumeCount)
            {
                throw new Exception($"最大消费者数量{maxConsumeCount} 不得小于 最小消费者数量{minConsumeCount}");
            }

            var queueInfo = await MQRealTimeStats.GetInfoByQueueAsync(_queueName);

            // 实时消息数量
            var currentMsgCount = queueInfo.Messages;

            // 实时消费者数量
            var currentConsumeCount = queueInfo.Consumers;

            // 需要消费者总数量
            var needTotalConsumeCount = (currentMsgCount / perConsumerMsgCount) + (currentMsgCount % perConsumerMsgCount > 0 ? 1 : 0);
            if (needTotalConsumeCount < minConsumeCount)
            {
                needTotalConsumeCount = minConsumeCount;
            }

            if (maxConsumeCount != 0 && needTotalConsumeCount > maxConsumeCount)
            {
                needTotalConsumeCount = maxConsumeCount;
            }

            // 加减数量
            int adjustCount = (int)(needTotalConsumeCount - currentConsumeCount);
            if (adjustCount > 0)
            {
                // 新增数量
                Console.WriteLine($"[ 消息: Ready={queueInfo.Messages_Ready}, Unacked={queueInfo.Messages_Unacknowledged}, Total={queueInfo.Messages} ] [ 消费者: Consumers={queueInfo.Consumers} +{adjustCount} ]");
                CreateConsumer(adjustCount);
            }
            else if (adjustCount < 0)
            {
                // 减少数量
                Console.WriteLine($"[ 消息: Ready={queueInfo.Messages_Ready}, Unacked={queueInfo.Messages_Unacknowledged}, Total={queueInfo.Messages} ] [ 消费者: Consumers={queueInfo.Consumers} {adjustCount} ]");
                RemoveConsumer(Math.Abs(adjustCount));
            }
        }
    }
}
