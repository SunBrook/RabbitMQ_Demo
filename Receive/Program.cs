using System;
using System.Threading;

namespace Receive
{
    internal class Program
    {
        static void Main(string[] args)
        {
            const string exChangeName = "test_exChange";
            const string queueName = "test_queue";
            const string routingKeyName = "test_routingKey";

            #region 1个线程，1个链接，1个消费者

            // 创建10个线程，1个线程接收一个消息
            for (int i = 0; i < 10; i++)
            {
                Thread thread = new Thread(() =>
                {
                    Consume.ReceivedMsg(Consume.Work, exChangeName, queueName, routingKeyName);
                });
                thread.Start();
            }

            #endregion

            #region 1个线程，1个链接，多个消费者

            // 并行数量
            ushort batchCount = 10;
            // 批量接收并行消费
            Consume.BatchReceivedMsg(Consume.Work, batchCount, exChangeName, queueName, routingKeyName);

            #endregion

            Console.Read();
        }
    }
}
