namespace NotifierSystemWorkerConsumer.Models
{
    public class AppSettings
    {
        public string Connection { get; set; }
        public string tgToken { get; set; }

        public string emailHost { get; set; }
        public string emailPort { get; set; }
        public string emailFrom { get; set; }
        public string emailFromPassword { get; set; }
    }
}
