using Confluent.Kafka;

namespace Consumer.Config
{
    public class KafkaSettings
    {
        public string BootstrapServers { get; set; }
        public string GroupId { get; set; }
        public string OmsOrderCreatedTopic { get; set; }
        public string OmsOrderStatusChangedTopic { get; set; }
        public int CollectBatchSize { get; set; }

        public int CollectTimeoutMs { get; set; }
        
        public AutoOffsetReset AutoOffsetReset { get; set; }
        public bool EnableAutoCommit { get; set; }
        public int SessionTimeoutMs { get; set; }
        public int HeartbeatIntervalMs { get; set; }
        public int MaxPollIntervalMs { get; set; }
    }
}