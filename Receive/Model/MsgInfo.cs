namespace Receive.Model
{
    public class MsgInfo
    {
        public int Id { get; set; }
        public string Message { get; set; }

        public override string ToString()
        {
            return $"Id = {Id}, Message = {Message}";
        }
    }
}
