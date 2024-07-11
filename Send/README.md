# RabbitMQ 生产与消费案例

## 准备

1. 安装好RabbitMQ环境，保证正常运行

2. 调整链接配置

   ```c#
   var factory = new ConnectionFactory
   {
       HostName = "hostName",		// 修改实际配置
       Port = 5672,
       UserName = "username",		// 修改实际配置
       Password = "password",		// 修改实际配置
       AutomaticRecoveryEnabled = true, // 自动重连
       RequestedFrameMax = uint.MaxValue,
       RequestedHeartbeat = TimeSpan.FromSeconds(60), // 心跳超时时间
   };
   ```



## 说明

```c#
/* Send 生产发送消息，可使用 Quartz 定时发送 */ 
// 马上生产
new ProduceJob().Work();

// 定时生产
QuartzFactory.CreateTask<ProduceJob>
   ("Produce", "ConsumesPull",
   "消耗拉取生产者定时任务：整点执行一次",
   "0 0 * * * ?", mqueueName).Wait();


/* Receivce 接收消费消息 */ 

// 一个消费者获取消息，并消费。
// 可创建多个线程，产生多个消费者消费数据
// 创建10个线程，1个线程接收一个消息
for (int i = 0; i < 10; i++)
{
    Thread thread = new Thread(() =>
    {
        Consume.ReceivedMsg(Consume.Work, exChangeName, queueName, routingKeyName);
    });
    thread.Start();
}


// 一个消费者预获取多条记录，并行消费。
// 并行数量
ushort batchCount = 10;
// 批量接收并行消费
Consume.BatchReceivedMsg(Consume.Work, batchCount, exChangeName, queueName, routingKeyName);
```

