﻿using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Text;

namespace Send
{
    public class ProduceJob : TaskJob
    {
        public override void Work()
        {
			try
			{
                Console.WriteLine($"生产消费开始：{DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                List<string> messages = new List<string>();
                for (int i = 0; i < 100; i++)
                {
                    messages.Add(i.ToString());
                }

                // 发送消息
                string exChangeName = "test_exChange";
                string queueName = "test_queue";
                string routingKeyName = "test_routingKey";
                SendMessages(messages, exChangeName, queueName, routingKeyName);

                Console.WriteLine($"生产消费完成：{DateTime.Now:yyyy-MM-dd HH:mm:ss}");
			}
			catch (Exception ex)
			{
                Console.WriteLine($"异常：{ex.Message}");
            }
        }

        public void SendMessages(List<string> messages, string exChangeName, string queueName, string routingKeyName)
        {
            try
            {
                using (var connection = MQInitForSendConsume.GetConnection())
                using (var channel = connection.CreateModel())
                {
                    // 创建一个新的，持久的交换区
                    channel.ExchangeDeclare(exChangeName, ExchangeType.Direct, true, false, null);
                    // 创建一个新的，持久的队列，没有排他性，不自动删除
                    channel.QueueDeclare(queueName, true, false, false, null);
                    // 绑定队列到交换区
                    channel.QueueBind(queueName, exChangeName, routingKeyName);
                    // 设置消息属性
                    var properties = channel.CreateBasicProperties();
                    // 消息是持久的，存在不会受服务器重启影响
                    properties.DeliveryMode = 2;

                    // 准备开始推送
                    var encoding = new UTF8Encoding();
                    foreach (var msg in messages)
                    {
                        try
                        {
                            var msgBytes = encoding.GetBytes(msg);
                            channel.BasicPublish(exChangeName, routingKeyName, properties, msgBytes);
                            Console.WriteLine($"生产：{msg}");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"生产异常：{ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"生产异常：{ex.Message}");
            }
        }
    }
}
