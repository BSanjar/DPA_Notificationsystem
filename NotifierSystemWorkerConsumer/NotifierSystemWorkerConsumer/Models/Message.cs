namespace NotifierSystemWorkerConsumer.Models
{
    public class Message
    {
        public string id { get; set; }
        public string processid { get; set; }
        public string created_at { get; set; }
        public string sended_at { get; set; }
        public string msg { get; set; }
        public string msgSub { get; set; }
        public string registry { get; set; }
        public string network { get; set; }
        public string sender_acc { get; set; }
        public string receiver_acc { get; set; }
        public string msg_status { get; set; }
    }
}
