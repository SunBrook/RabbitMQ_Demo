using Newtonsoft.Json;
using Receive.Model;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Receive.Consume
{
    /// <summary>
    /// 获取MQ队列信息
    /// </summary>
    public class MQRealTimeStats
    {
        public static async Task<QueueInfo> GetInfoByQueueAsync(string queueName)
        {
            string user = MQInit.User;
            string password = MQInit.Password;
            string host = MQInit.Host;
            int port = MQInit.WebPort;
            string virtualHost = MQInit.VirtualHost;

            var client = new HttpClient
            {
                BaseAddress = new Uri($"http://{host}:{port}/api/queues/{virtualHost}/")
            };
            client.DefaultRequestHeaders.Add("Authorization", $"Basic {Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($"{user}:{password}"))}");

            try
            {
                var response = await client.GetAsync(queueName);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var queueInfo = JsonConvert.DeserializeObject<QueueInfo>(content);
                    return queueInfo;
                }
                else
                {
                    // Handle error response
                    Console.WriteLine(response.Content.ReadAsStringAsync().Result);
                    return null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }
        }
    }
}
