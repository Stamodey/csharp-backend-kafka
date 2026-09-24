using Confluent.Kafka;

namespace WebApi.Config
{
    public class KafkaSettings
    {
        public string BootstrapServers { get; set; }
        public string ClientId { get; set; }
        public string OmsOrderCreatedTopic { get; set; }
        public string OmsOrderStatusChangedTopic { get; set; }
        
        public CompressionType CompressionType { get; set; }
        public Partitioner Partitioner { get; set; } 
        public Acks Acks { get; set; } 
        public bool EnableIdempotence { get; set; } 
        public int LingerMs { get; set; }       
        public int BatchSize { get; set; }     
        public int MaxInFlight { get; set; }
    }
}