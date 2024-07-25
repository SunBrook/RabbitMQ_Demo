using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Receive.Model;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Receive.Consume
{
    public class ControlCustomConsumeSingle
    {
        private readonly string _exChangeName;
        private readonly string _queueName;
        private readonly string _routingKeyName;

        private readonly Action<string> _action;

        // 单条链接对应的信道数量
        private int _channelCount;
        // 单条信道对应的消费者数量
        private int _consumerCount;

        private ConcurrentBag<ConnectChannel> _ConnectChannels;


        public ControlCustomConsumeSingle(int channelCount, int consumerCount, string exChangeName, string queueName, string routingKeyName, Action<string> action)
        {
            _channelCount = channelCount;
            _consumerCount = consumerCount;
            _exChangeName = exChangeName;
            _queueName = queueName;
            _routingKeyName = routingKeyName;
            _action = action;

            _ConnectChannels = new ConcurrentBag<ConnectChannel>();
        }

        /// <summary>
        /// 单条消费 - 创建消费者
        /// </summary>
        /// <param name="count">添加消费者数量</param>
        public void CreateConsumer(int count)
        {
            // 补充已有信道的消费者
            foreach (var connectItem in _ConnectChannels)
            {
                foreach (var channelItem in connectItem.ChannelConsumes)
                {
                    var addCount = _consumerCount - channelItem.AliveCustomerTags.Count;
                    while (addCount > 0)
                    {
                        var consumer = new EventingBasicConsumer(channelItem.Channel);
                        consumer.Received += (model, ea) =>
                        {
                            var body = ea.Body.ToArray();
                            var message = Encoding.UTF8.GetString(body);
                            _action(message);

                            // 手动应答消息
                            channelItem.Channel.BasicAck(ea.DeliveryTag, false);
                        };

                        var tag = Guid.NewGuid().ToString("N");
                        channelItem.AliveCustomerTags.Add(tag);
                        channelItem.Channel.BasicConsume(queue: _queueName, autoAck: false, consumer: consumer, consumerTag: tag);
                        addCount--;
                        count--;

                        Console.WriteLine($"创建消费者：{tag}");

                        if (count == 0)
                        {
                            break;
                        }
                    }

                    if (count == 0)
                    {
                        break;
                    }
                }

                if (count == 0)
                {
                    break;
                }
            }

            // 创建新的链接和信道和消费者
            while (count > 0)
            {
                var connectChannel = new ConnectChannel
                {
                    Connection = MQInitForReceivedConsume.GetConnection(),
                    ChannelConsumes = new List<ChannelConsumeStatus>()
                };

                for (int i = 0; i < _channelCount; i++)
                {
                    var channelConsumeStatus = new ChannelConsumeStatus
                    {
                        Channel = connectChannel.Connection.CreateModel(),
                        AliveCustomerTags = new List<string>()
                    };

                    // 指示通道不预取超过1个消息
                    channelConsumeStatus.Channel.BasicQos(0, 1, false);

                    // 创建一个新的，持久的交换区
                    channelConsumeStatus.Channel.ExchangeDeclare(_exChangeName, ExchangeType.Direct, true, false, null);
                    // 创建一个新的，持久的队列
                    channelConsumeStatus.Channel.QueueDeclare(_queueName, true, false, false, null);
                    // 绑定队列到交换区
                    channelConsumeStatus.Channel.QueueBind(_queueName, _exChangeName, _routingKeyName);

                    for (int j = 0; j < _consumerCount; j++)
                    {
                        var consumer = new EventingBasicConsumer(channelConsumeStatus.Channel);

                        consumer.Received += (model, ea) =>
                        {
                            var body = ea.Body.ToArray();
                            var message = Encoding.UTF8.GetString(body);
                            _action(message);

                            // 手动应答消息
                            channelConsumeStatus.Channel.BasicAck(ea.DeliveryTag, false);
                        };

                        var tag = Guid.NewGuid().ToString("N");
                        channelConsumeStatus.AliveCustomerTags.Add(tag);
                        channelConsumeStatus.Channel.BasicConsume(queue: _queueName, autoAck: false, consumer: consumer, consumerTag: tag);
                        count--;

                        Console.WriteLine($"创建消费者：{tag}");

                        if (count == 0)
                        {
                            break;
                        }
                    }

                    connectChannel.ChannelConsumes.Add(channelConsumeStatus);

                    if (count == 0)
                    {
                        break;
                    }
                }

                _ConnectChannels.Add(connectChannel);
            }
        }

        public void RemoveConsumer(int count)
        {
            foreach (var connectItem in _ConnectChannels.Where(t => t.Connection.IsOpen))
            {
                foreach (var channelItem in connectItem.ChannelConsumes.Where(t => t.Channel.IsOpen))
                {
                    var removeTags = new List<string>();
                    foreach (var tag in channelItem.AliveCustomerTags)
                    {
                        channelItem.Channel.BasicCancel(tag);
                        removeTags.Add(tag);
                        count--;

                        Console.WriteLine($"删除消费者：{tag}");

                        if (count == 0)
                        {
                            break;
                        }
                    }

                    channelItem.AliveCustomerTags = channelItem.AliveCustomerTags.Except(removeTags).ToList();
                    if (count == 0)
                    {
                        break;
                    }
                }

                if (count == 0)
                {
                    break;
                }
            }

        }

        private void ClearEmpty()
        {
            foreach (var connectItem in _ConnectChannels.Where(t => t.Connection.IsOpen))
            {
                foreach (var channelItem in connectItem.ChannelConsumes.Where(t => t.Channel.IsOpen))
                {
                    if (channelItem.AliveCustomerTags.Count == 0 &&
                        channelItem.Channel.ConsumerCount(_queueName) == 0 &&
                        channelItem.Channel.IsOpen)
                    {
                        Console.WriteLine($"关闭信道");
                        channelItem.Channel.Close();
                        channelItem.Channel.Dispose();
                    }
                }

                if (!connectItem.ChannelConsumes.Exists(t => t.Channel.IsOpen))
                {
                    Console.WriteLine($"关闭链接: {connectItem.Connection.ClientProvidedName}");
                    connectItem.Connection.Close();
                    connectItem.Connection.Dispose();
                }
            }

            // 清理断开链接的数据
            var openConnections = _ConnectChannels.Where(t => t.Connection.IsOpen).ToList();
            var newBag = new ConcurrentBag<ConnectChannel>();
            foreach (var item in openConnections)
            {
                newBag.Add(item);
            }
            _ConnectChannels = newBag;
        }

        /// <summary>
        /// 调整消费者数量，每个消费者一次只处理一条消息
        /// </summary>
        /// <param name="perConsumerMsgCount">每个消费者对应多少个消息数量</param>
        /// <param name="maxConsumeCount">最大消费者数量，默认0，代表消费者数量无上限</param>
        /// <param name="minConsumeCount">最小消费者数量，默认0，代表在没有消息时没有消费者</param>
        public async void AdjustConsumeCount(uint perConsumerMsgCount, uint maxConsumeCount = 0, uint minConsumeCount = 0)
        {
            // 清理闲置链接
            ClearEmpty();

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
