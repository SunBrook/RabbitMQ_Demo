namespace Receive.Model
{
    public class QueueInfo
    {
        public int Messages { get; set; }
        public int Messages_Ready { get; set; }
        public int Messages_Unacknowledged { get; set; }
        public int Consumers { get; set; }
    }
}
