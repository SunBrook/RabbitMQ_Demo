using RabbitMQ.Client;
using System.Collections.Generic;

namespace Receive.Model
{
    public class ConnectChannel
    {
        public IConnection Connection { get; set; }
        public List<ChannelConsumeStatus> ChannelConsumes { get; set; }
    }

    public class ChannelConsumeStatus
    {
        public IModel Channel { get; set; }
        public List<string> AliveCustomerTags { get; set; }
    }
}
