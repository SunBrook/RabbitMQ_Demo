using RabbitMQ.Client;
using System;

namespace Send.Model
{
    public static class MQInitForSendConsume
    {
        private static IConnection sharedConnection;
        private static int ChannelCount { get; set; }
        private static readonly object _locker = new object();

        /// <summary>
        /// 用于生产端共用一个链接，生产完全释放
        /// </summary>
        public static IConnection SharedConnection
        {
            get
            {
                if (ChannelCount >= 10000)
                {
                    if (sharedConnection != null && sharedConnection.IsOpen)
                    {
                        sharedConnection.Close();
                    }
                    sharedConnection = null;
                    ChannelCount = 0;
                }
                if (sharedConnection == null)
                {
                    lock (_locker)
                    {
                        if (sharedConnection == null)
                        {
                            sharedConnection = GetConnection();
                            ChannelCount++;
                        }
                    }
                }
                return sharedConnection;
            }
        }

        public static IConnection GetConnection()
        {
            var factory = new ConnectionFactory
            {
                HostName = MQInit.Host,
                Port = MQInit.ClientPort,
                UserName = MQInit.User,
                Password = MQInit.Password,
                AutomaticRecoveryEnabled = true, // 自动重连
                RequestedFrameMax = uint.MaxValue,
                RequestedHeartbeat = TimeSpan.FromSeconds(60), // 心跳超时时间
            };

            // 设置客户端名称（方便识别多个客户端，强烈建议设置）
            lock (_locker)
            {
                factory.ClientProvidedName = $"customer_{Guid.NewGuid():N}";
            }

            return factory.CreateConnection();
        }
    }
}
