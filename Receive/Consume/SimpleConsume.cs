using RabbitMQ.Client.Events;
using RabbitMQ.Client;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Receive.Model;

namespace Receive.Consume
{
    public class SimpleConsume
    {
        /// <summary>
        /// 单个消费
        /// </summary>
        /// <param name="action"></param>
        /// <param name="exChangeName"></param>
        /// <param name="queueName"></param>
        /// <param name="routingKeyName"></param>
        public static void ReceivedMsg(Action<string> action, string exChangeName, string queueName, string routingKeyName)
        {
            try
            {
                using (var connection = MQInitForReceivedConsume.SharedConnection)
                using (var channel = connection.CreateModel())
                {
                    // 指示通道不预取超过1个消息
                    channel.BasicQos(0, 1, false);

                    // 创建一个新的，持久的交换区
                    channel.ExchangeDeclare(exChangeName, ExchangeType.Direct, true, false, null);
                    // 创建一个新的，持久的队列
                    channel.QueueDeclare(queueName, true, false, false, null);
                    // 绑定队列到交换区
                    channel.QueueBind(queueName, exChangeName, routingKeyName);

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

                    while (true)
                    {
                        Thread.Sleep(1);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"接收消息异常：{ex}");
            }
        }

        /// <summary>
        /// 并行消费
        /// </summary>
        /// <param name="action"></param>
        /// <param name="batchCount"></param>
        /// <param name="exChangeName"></param>
        /// <param name="queueName"></param>
        /// <param name="routingKeyName"></param>
        public static void BatchReceivedMsg(Action<string> action, ushort batchCount, string exChangeName, string queueName, string routingKeyName)
        {
            try
            {
                using (var connection = MQInitForReceivedConsume.GetConnectionBatch(batchCount))
                using (var channel = connection.CreateModel())
                {
                    // 指示通道不预取超过batchCount个消息
                    channel.BasicQos(0, batchCount, false);

                    // 创建一个新的，持久的交换区
                    channel.ExchangeDeclare(exChangeName, ExchangeType.Direct, true, false, null);
                    // 创建一个新的，持久的队列
                    channel.QueueDeclare(queueName, true, false, false, null);
                    // 绑定队列到交换区
                    channel.QueueBind(queueName, exChangeName, routingKeyName);

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

                    while (true)
                    {
                        Thread.Sleep(1);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"接收消息异常：{ex}");
            }
        }
    }
}
