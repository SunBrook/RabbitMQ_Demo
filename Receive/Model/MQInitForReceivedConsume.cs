using RabbitMQ.Client;
using Receive.Model;
using System;

namespace Receive.Model
{
    public static class MQInitForReceivedConsume
    {
        private static IConnection sharedConnection;
        private static int ChannelCount { get; set; }
        private static readonly object _locker = new object();

        /// <summary>
        /// 用于消费端共用一个链接
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



        /// <summary>
        /// 获取批量获取消息消费的链接
        /// </summary>
        /// <param name="batchCount"></param>
        /// <returns></returns>
        public static IConnection GetConnectionBatch(ushort batchCount)
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

            factory.ConsumerDispatchConcurrency = batchCount;
            // 设置客户端名称（方便识别多个客户端，强烈建议设置）

            lock (_locker)
            {
                factory.ClientProvidedName = $"customer_{Guid.NewGuid():N}";
            }
            
            factory.DispatchConsumersAsync = true;

            return factory.CreateConnection();
        }
    }
}
