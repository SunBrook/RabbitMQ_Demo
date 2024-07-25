using Newtonsoft.Json;
using Receive.Model;
using Receive.Tool;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Receive
{
    /// <summary>
    /// 处理消费
    /// </summary>
    public class Dispose
    {
        public static void Work(string message)
        {
            try
            {
                Console.WriteLine($"消费消息：{message}");

                // 反序列化
                var model = JsonConvert.DeserializeObject<MsgInfo>(message);

                // 随机等待时间
                var waitSec = new Random().Next(0, 10);
                Task.Delay(waitSec * 1000).Wait();

                var filePath = Path.Combine(System.Environment.CurrentDirectory, "Receive.log");
                //FileKit.Instance.Log(filePath, $"消费：{message}");

                using (Log log = new Log(filePath))
                {
                    log.Write(model.ToString());
                }

                Console.WriteLine($"消费完成：{message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"消费异常：{ex.Message}");
            }
        }
    }
}
