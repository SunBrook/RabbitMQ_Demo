using Quartz.Impl;
using Quartz;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Send
{
    public class QuartzFactory
    {
        /// <summary>
        /// 定时任务公共方法
        /// </summary>
        /// <typeparam name="T">任务job，需要继承 IJob</typeparam>
        /// <param name="IdName">任务名称</param>
        /// <param name="IdGroup">任务分组</param>
        /// <param name="description">备注说明</param>
        /// <param name="cornExpression">corn 表达式</param>
        /// <param name="mqueueName">消息队列名称，可空</param>
        /// <returns></returns>
        public static async Task CreateTask<T>(string IdName, string IdGroup,
            string description, string cornExpression, string mqueueName = null)
            where T : TaskJob
        {
            var schedulerFactory = new StdSchedulerFactory();
            var scheduler = await schedulerFactory.GetScheduler();
            await scheduler.Start();
            Console.WriteLine($"创建作业开始：{description}");

            var jobDetail = mqueueName == null ?
                JobBuilder.Create<T>()
                .StoreDurably(true) //孤立存储
                .RequestRecovery(true)  // 崩溃后重新执行
                .WithIdentity(IdName, IdGroup)
                .WithDescription(description)
                .DisallowConcurrentExecution(true)  // 禁止并行执行
                .PersistJobDataAfterExecution(true) // 持久化jobData
                .Build()
                :
                JobBuilder.Create<T>()
                .SetJobData(new JobDataMap()
                {
                    new KeyValuePair<string, object>("MQueueName", mqueueName)
                })
                .StoreDurably(true) //孤立存储
                .RequestRecovery(true)  // 崩溃后重新执行
                .WithIdentity(IdName, IdGroup)
                .WithDescription(description)
                .DisallowConcurrentExecution(true)  // 禁止并行执行
                .PersistJobDataAfterExecution(true) // 持久化jobData
                .Build();

            // 创建触发器
            var trigger = TriggerBuilder.Create()
                .WithCronSchedule(cornExpression)
                .Build();

            // 添加调度
            await scheduler.ScheduleJob(jobDetail, trigger);
            Console.WriteLine("任务调度创建完成");
        }

        /// <summary>
        /// 定时任务公共方法
        /// </summary>
        /// <typeparam name="T">任务job，需要继承 IJob</typeparam>
        /// <param name="IdName">任务名称</param>
        /// <param name="IdGroup">任务分组</param>
        /// <param name="description">备注说明</param>
        /// <param name="cornExpression">corn 表达式</param>
        /// <param name="obj">自定义入参对象Parameter</param>
        /// <param name="mqueueName">消息队列名称，可空</param>
        /// <returns></returns>
        public static async Task CreateTask<T, T2>(string IdName, string IdGroup,
            string description, string cornExpression, T2 obj, string mqueueName = null)
            where T : TaskJob
        {
            var schedulerFactory = new StdSchedulerFactory();
            var scheduler = await schedulerFactory.GetScheduler();
            await scheduler.Start();
            Console.WriteLine($"创建作业开始：{description}");

            var jobDetail = mqueueName == null ?
                JobBuilder.Create<T>()
                .SetJobData(new JobDataMap()
                {
                    new KeyValuePair<string, object>("Parameter",obj)
                })
                .StoreDurably(true) //孤立存储
                .RequestRecovery(true)  // 崩溃后重新执行
                .WithIdentity(IdName, IdGroup)
                .WithDescription(description)
                .DisallowConcurrentExecution(true)  // 禁止并行执行
                .PersistJobDataAfterExecution(true) // 持久化jobData
                .Build()
                :
                JobBuilder.Create<T>()
                .SetJobData(new JobDataMap()
                {
                    new KeyValuePair<string, object>("MQueueName", mqueueName),
                    new KeyValuePair<string, object>("Parameter",obj)
                })
                .StoreDurably(true) //孤立存储
                .RequestRecovery(true)  // 崩溃后重新执行
                .WithIdentity(IdName, IdGroup)
                .WithDescription(description)
                .DisallowConcurrentExecution(true)  // 禁止并行执行
                .PersistJobDataAfterExecution(true) // 持久化jobData
                .Build();

            // 创建触发器
            var trigger = TriggerBuilder.Create()
                .WithCronSchedule(cornExpression)
                .Build();

            // 添加调度
            await scheduler.ScheduleJob(jobDetail, trigger);
            Console.WriteLine("任务调度创建完成");
        }
    }
}
