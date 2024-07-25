using Receive.Consume;
using System;
using System.Threading;

namespace Receive
{
    internal class Program
    {
        static void Main(string[] args)
        {
            //// 简单消费案例
            //SimpleConsumeTest();
            //return;

            //// 单线消费者数量控制
            //SimpleConsumeCtlTest();
            //return;

            //// 灵活配置消费信息
            //CustomConsumeTest();
            //return;

            // 灵活配置消费数量控制
            CustomConsumeCtlTest();
            return;
        }

        private static void SimpleConsumeTest()
        {
            const string exChangeName = "test_exChange";
            const string queueName = "test_queue";
            const string routingKeyName = "test_routingKey";

            #region 1个线程，1个链接, 10 个频道，1个消费者

            // 创建10个线程，1个线程接收一个消息
            //for (int i = 0; i < 10; i++)
            //{
            //    Thread thread = new Thread(() =>
            //    {
            //        SimpleConsume.ReceivedMsg(Dispose.Work, exChangeName, queueName, routingKeyName);
            //    });
            //    thread.Start();
            //}

            #endregion

            #region 1个线程，1个链接, 1个频道，1个消费者，多个消费

            // 并行数量
            ushort batchCount = 10;
            // 批量接收并行消费
            //for (int i = 0; i < 10; i++)
            //{
            //    Thread thread = new Thread(() =>
            //    {
            //        SimpleConsume.BatchReceivedMsg(Dispose.Work, batchCount, exChangeName, queueName, routingKeyName);
            //    });
            //    thread.Start();
            //}

            SimpleConsume.BatchReceivedMsg(Dispose.Work, batchCount, exChangeName, queueName, routingKeyName);

            #endregion
        }

        private static void SimpleConsumeCtlTest()
        {
            // 监控消息数量，根据消息数量动态增减消费者
            const string exChangeName = "test_exChange";
            const string queueName = "test_queue";
            const string routingKeyName = "test_routingKey";

            //ControlConsumeSingle adjustConsume = new ControlConsumeSingle(exChangeName, queueName, routingKeyName, Dispose.Work);
            //while (true)
            //{
            //    adjustConsume.AdjustConsumeCount(2, 50);
            //    Thread.Sleep(10000);
            //}

            const ushort consumeCount = 10;
            ControlConsumeBatch controlConsumeBatch = new ControlConsumeBatch(consumeCount, exChangeName, queueName, routingKeyName, Dispose.Work);
            while (true)
            {
                controlConsumeBatch.AdjustConsumeCount(2, 50);
                Thread.Sleep(10000);
            }
        }

        private static void CustomConsumeTest()
        {
            const string exChangeName = "test_exChange";
            const string queueName = "test_queue";
            const string routingKeyName = "test_routingKey";

            //// 2个链接，每个链接5个频道，每个频道10个消费者
            //CustomConsume.ReceivedMsg(2, 5, 10, Dispose.Work, exChangeName, queueName, routingKeyName);

            // 2个链接，每个链接5个频道，每个频道10个消费者，每个消费者并行消费10条信息
            CustomConsume.BatchReceivedMsg(2, 5, 10, 10, Dispose.Work, exChangeName, queueName, routingKeyName);
        }

        private static void CustomConsumeCtlTest()
        {
            const string exChangeName = "test_exChange";
            const string queueName = "test_queue";
            const string routingKeyName = "test_routingKey";

            //// 每个链接2个频道，每个频道5个消费者
            //ControlCustomConsumeSingle adjustSingleConsume = new ControlCustomConsumeSingle(2, 5, exChangeName, queueName, routingKeyName, Dispose.Work);
            //while (true)
            //{
            //    adjustSingleConsume.AdjustConsumeCount(2, 50);
            //    Thread.Sleep(10000);
            //}

            // 每个链接2个频道，每个频道5个消费者，每个消费者并行处理5条消息
            ControlCustomConsumeBatch adjustBatchConsume = new ControlCustomConsumeBatch(2, 5, 5, exChangeName, queueName, routingKeyName, Dispose.Work);
            while (true)
            {
                adjustBatchConsume.AdjustConsumeCount(2, 50);
                Thread.Sleep(10000);
            }
        }
    }
}
