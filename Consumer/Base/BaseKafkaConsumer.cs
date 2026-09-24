using Common;
using Confluent.Kafka;
using Consumer.Config;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Consumer.Base
{
    public abstract class BaseKafkaConsumer<T>: IHostedService
    where T : class
    {
        private readonly IConsumer<string, string> _consumer;
        private readonly ILogger<BaseKafkaConsumer<T>> _logger;
        private readonly string _topic;
        
        protected BaseKafkaConsumer(
            IOptions<KafkaSettings> kafkaSettings,
            string topic,
            ILogger<BaseKafkaConsumer<T>> logger)
        {
            var config = new ConsumerConfig
            {
                BootstrapServers = kafkaSettings.Value.BootstrapServers,
                GroupId = kafkaSettings.Value.GroupId,
                AutoOffsetReset = AutoOffsetReset.Latest,
                EnableAutoCommit = true,
                AutoCommitIntervalMs = 5_000,
                SessionTimeoutMs = 60_000,
                HeartbeatIntervalMs = 3_000,
                MaxPollIntervalMs = 300_000
            };

            _logger = logger;
            _topic = topic;
            _consumer = new ConsumerBuilder<string, string>(config).Build();
        }
        
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await StartConsuming(_topic, cancellationToken);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            StopConsuming();
            return Task.CompletedTask;
        }

        private async Task StartConsuming(string topic, CancellationToken cancellationToken)
        {
            _consumer.Subscribe(topic);
            _logger.LogInformation($"Started consuming from topic: {topic}");

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var consumeResult = _consumer.Consume(cancellationToken);

                    var msg = new Message<T>
                    {
                        Key = consumeResult.Message.Key,
                        Body = consumeResult.Message.Value.FromJson<T>()
                    };
                    
                    if (consumeResult.Message != null)
                    {
                        try
                        {
                            await ProcessMessage(msg, cancellationToken);
                            _consumer.Commit(consumeResult);
                        }
                        catch (Exception e)
                        {
                            _logger.LogError(e, "Error processing message");
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Consumer cancelled");
            }
            catch (ConsumeException ex)
            {
                _logger.LogError(ex, "Consume error occurred");
            }
            finally
            {
                StopConsuming();
            }
        }
        
        private void StopConsuming()
        {
            _logger.LogInformation($"Stopping consuming from topic: {_topic}");
            _consumer.Close();
            _consumer.Dispose();
        }

        protected abstract Task ProcessMessage(Message<T> message, CancellationToken token);
    }
}