using System.Configuration;

namespace Receive.Model
{
    public class MQInit
    {
#if DEBUG
        public const string User = "XXXX";
        public const string Password = "XXXXX";
        public const string Host = "XX.XX.X.XX";
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
}
