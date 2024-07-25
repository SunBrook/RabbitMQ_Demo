# RabbitMQ 生产与消费案例



# 准备

1. 安装好RabbitMQ环境，保证正常运行

2. 调整配置，包含本地账户配置和线上正式环境配置

   1. 客户端信息：User、Password、Host、ClientPort
   2. 网站登录信息（账户密码权限，可以单独配置账号密码）：User、Password、Host、WebPort、VirtualHost（点击任意链接，查看浏览器地址栏）

   ```c#
       public class MQInit
       {
   #if DEBUG
           public const string User = "";	// 账户
           public const string Password = "";	// 密码
           public const string Host = "xx.xx.xx.xx"; // 地址
           public const int WebPort = 15672;
           public const int ClientPort = 5672;
           public const string VirtualHost = "%2F";
   #else
           public static readonly string User = ConfigurationManager.AppSettings["MQ_User"];
           public static readonly string Password = ConfigurationManager.AppSettings["MQ_Password"];
           public static readonly string Host = ConfigurationManager.AppSettings["MQ_Host"];
           public static readonly int WebPort = int.Parse(ConfigurationManager.AppSettings["MQ_WebPort"]);
           public static readonly int ClientPort = int.Parse(ConfigurationManager.AppSettings["MQ_ClientPort"]);
           public static readonly string VirtualHost = ConfigurationManager.AppSettings["MQ_VirtualHost"];
   #endif
       }
   ```

   ```xml
   <appSettings>
   	<add key="MQ_User" value="" />
   	<add key="MQ_Password" value="" />
   	<add key="MQ_Host" value="" />
   	<add key="MQ_WebPort" value="" />
   	<add key="MQ_ClientPort" value="" />
   	<add key="MQ_VirtualHost" value="" />
   </appSettings>
   ```






# 生产

```c#
/* Send 生产发送消息，可使用 Quartz 定时发送 */ 
// 马上生产
new ProduceJob().Work();

// 定时生产
QuartzFactory.CreateTask<ProduceJob>
   ("Produce", "ConsumesPull",
   "消耗拉取生产者定时任务：整点执行一次",
   "0 0 * * * ?", mqueueName).Wait();
```



# 消费



## 单线，固定消费者数量

```c#
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
```



## 单线，监听MQ，根据消息数量动态改变消费数量

```c#
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
```





## 配置，多链接、多信道、多消费者，固定

```c#
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
```





## 配置，多链接、多信道、多消费者，监听MQ，根据消息数量动态改变消费数量，调整信道、链接数量

```c#
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
```

