using System;

namespace Send
{
    internal class Program
    {
        static void Main(string[] args)
        {
            new ProduceJob().Work();

            //QuartzFactory.CreateTask<ProduceJob>
            //   ("Produce", "ConsumesPull",
            //   "消耗拉取生产者定时任务：整点执行一次",
            //   "0 0 * * * ?", mqueueName).Wait();

            Console.WriteLine("任务调度添加完成");
            Console.Read();
        }
    }
}
