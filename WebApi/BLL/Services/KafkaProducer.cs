using Common;
using Confluent.Kafka;
using Microsoft.Extensions.Options;
using WebApi.Config;

namespace WebApi.BLL.Services
{
    public class KafkaProducer : IDisposable
    {
        private readonly IProducer<string, string> _producer;
        private bool _disposed;

        public KafkaProducer(IOptions<KafkaSettings> kafkaSettings)
        {
            
            var config = new ProducerConfig
            {
                BootstrapServers = kafkaSettings.Value.BootstrapServers,
                ClientId = kafkaSettings.Value.ClientId,
                CompressionType = CompressionType.Snappy,
                Partitioner = Partitioner.Consistent,
                Acks = Acks.All,
                LingerMs = 10,
                BatchSize = 65536,
                EnableIdempotence = true,
                MaxInFlight = 5
            };

            _producer = new ProducerBuilder<string, string>(config).Build();
        }

        public async Task Produce<T>(string topic, (string key, T message)[] messages, CancellationToken token)
        {
            var tasks = messages.Select(async message =>
            {
                try
                {
                    return await _producer.ProduceAsync(topic,
                        new Message<string, string>
                        {
                            Key = message.key,
                            Value = message.message.ToJson()
                        }, token);
                }
                catch (ProduceException<string, string> ex)
                {
                    Console.WriteLine($"Failed to send message: {ex.Error.Reason}");
                    return null;
                }
            });

            var results = await Task.WhenAll(tasks);

            if (results.Any(x => x is null))
            {
                throw new Exception("Failed to produce messages");
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing)
            {
                return;
            }
            
            try
            {
                _producer.Flush(TimeSpan.FromSeconds(10));
                _producer.Dispose();
            }
            catch (Exception e)
            {
                // ignore
            }
        }
        
        ~KafkaProducer()
        {
            Dispose(false);
        }
    }
}