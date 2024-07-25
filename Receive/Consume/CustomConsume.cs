using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Receive.Model;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Receive.Consume
{
    /// <summary>
    /// 配置化消费
    /// </summary>
    public class CustomConsume
    {
        /// <summary>
        /// 消费者单个消费
        /// </summary>
        /// <param name="connectionCount">链接数量</param>
        /// <param name="channelCount">信道数量</param>
        /// <param name="consumeCount">消费者数量</param>
        /// <param name="action">回调方法</param>
        /// <param name="exChangeName">exChange</param>
        /// <param name="queueName">queue</param>
        /// <param name="routingKeyName">routingKey</param>
        public static void ReceivedMsg(int connectionCount, int channelCount, int consumeCount, Action<string> action, string exChangeName, string queueName, string routingKeyName)
        {
            try
            {
                for (int p = 0; p < connectionCount; p++)
                {
                    var connection = MQInitForReceivedConsume.GetConnection();
                    for (int i = 0; i < channelCount; i++)
                    {
                        var channel = connection.CreateModel();
                        // 指示通道不预取超过1个消息
                        channel.BasicQos(0, 1, false);

                        // 创建一个新的，持久的交换区
                        channel.ExchangeDeclare(exChangeName, ExchangeType.Direct, true, false, null);
                        // 创建一个新的，持久的队列
                        channel.QueueDeclare(queueName, true, false, false, null);
                        // 绑定队列到交换区
                        channel.QueueBind(queueName, exChangeName, routingKeyName);

                        for (int j = 0; j < consumeCount; j++)
                        {
                            // 定义这个队列的消费者
                            var consumer = new EventingBasicConsumer(channel);

                            consumer.Received += (model, ea) =>
                            {
                                var body = ea.Body.ToArray();
                                var message = Encoding.UTF8.GetString(body);
                                action(message);

                                // 手动应答消息
                                channel.BasicAck(ea.DeliveryTag, false);
                            };

                            channel.BasicConsume(queue: queueName, autoAck: false, consumer: consumer);
                        }
                    }
                }
                while (true)
                {
                    Thread.Sleep(1);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"接收消息异常：{ex}");
            }
        }

        /// <summary>
        ///  消费者并行消费
        /// </summary>
        /// <param name="connectionCount">链接数量</param>
        /// <param name="channelCount">信道数量</param>
        /// <param name="consumeCount">消费者数量</param>
        /// <param name="batchCount">并行消费数量</param>
        /// <param name="action">回调方法</param>
        /// <param name="exChangeName">exChange</param>
        /// <param name="queueName">queue</param>
        /// <param name="routingKeyName">routingKey</param>
        public static void BatchReceivedMsg(int connectionCount, int channelCount, int consumeCount, ushort batchCount, Action<string> action, string exChangeName, string queueName, string routingKeyName)
        {
            try
            {
                for (int p = 0; p < connectionCount; p++)
                {
                    var connection = MQInitForReceivedConsume.GetConnectionBatch(batchCount);
                    for (int i = 0; i < channelCount; i++)
                    {
                        var channel = connection.CreateModel();
                        // 指示通道不预取超过batchCount个消息
                        channel.BasicQos(0, batchCount, false);

                        // 创建一个新的，持久的交换区
                        channel.ExchangeDeclare(exChangeName, ExchangeType.Direct, true, false, null);
                        // 创建一个新的，持久的队列
                        channel.QueueDeclare(queueName, true, false, false, null);
                        // 绑定队列到交换区
                        channel.QueueBind(queueName, exChangeName, routingKeyName);

                        for (int j = 0; j < consumeCount; j++)
                        {
                            // 定义这个队列的消费者
                            var consumer = new AsyncEventingBasicConsumer(channel);

                            consumer.Received += async (model, ea) =>
                            {
                                await Task.Run(() =>
                                {
                                    var body = ea.Body.ToArray();
                                    var message = Encoding.UTF8.GetString(body);
                                    action(message);

                                    // 手动应答消息
                                    lock (channel)
                                    {
                                        channel.BasicAck(ea.DeliveryTag, false);
                                    }
                                });
                            };

                            lock (channel)
                            {
                                channel.BasicConsume(queue: queueName, autoAck: false, consumer: consumer);
                            }
                        }
                    }
                }

                while (true)
                {
                    Thread.Sleep(1);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"接收消息异常：{ex}");
            }
        }
    }
}
