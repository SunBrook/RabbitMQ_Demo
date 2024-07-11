using Quartz;
using System;
using System.Threading.Tasks;

namespace Send
{
    public class TaskJob : IJob
    {
        public Task Execute(IJobExecutionContext context)
        {
            return Task.Factory.StartNew(() =>
            {
                Work();
            });
        }

        /// <summary>
        /// 方法主体
        /// </summary>
        public virtual void Work()
        {
            throw new NotImplementedException("需要重写该方法");
        }
    }
}
