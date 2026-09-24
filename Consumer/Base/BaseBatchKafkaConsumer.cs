using Common;
using Confluent.Kafka;
using Consumer.Config;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Consumer.Base
{
    public abstract class BaseBatchKafkaConsumer<T> : IHostedService
        where T : class
    {
        private readonly IConsumer<string, string> _consumer;
        private readonly ILogger<BaseBatchKafkaConsumer<T>> _logger;
        private readonly string _topic;
        private readonly int _batchSize;
        private readonly int _collectTimeoutMs;
        
        protected BaseBatchKafkaConsumer(
            IOptions<KafkaSettings> kafkaSettings,
            string topic,
            ILogger<BaseBatchKafkaConsumer<T>> logger)
        {
            // Используем ВСЕ параметры из KafkaSettings
            var config = new ConsumerConfig
            {
                BootstrapServers = kafkaSettings.Value.BootstrapServers,
                GroupId = kafkaSettings.Value.GroupId,
                AutoOffsetReset = AutoOffsetReset.Latest,
                EnableAutoCommit = false,
                SessionTimeoutMs = 60_000,
                HeartbeatIntervalMs = 3_000,
                MaxPollIntervalMs = 300_000
            };

            _logger = logger;
            _topic = topic;
            _batchSize = kafkaSettings.Value.CollectBatchSize;
            _collectTimeoutMs = kafkaSettings.Value.CollectTimeoutMs;
            _consumer = new ConsumerBuilder<string, string>(config).Build();
        }
        
        // Остальной код без изменений...
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await StartBatchConsuming(_topic, cancellationToken);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            StopConsuming();
            return Task.CompletedTask;
        }

        private async Task StartBatchConsuming(string topic, CancellationToken cancellationToken)
        {
            _consumer.Subscribe(topic);
            _logger.LogInformation($"Started BATCH consuming from topic: {topic}, BatchSize: {_batchSize}, TimeoutMs: {_collectTimeoutMs}");

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var consumeResults = CollectBatch(cancellationToken);
                    
                    if (consumeResults.Any())
                    {
                        try
                        {
                            // Преобразуем ConsumeResult в Message<T>
                            var messages = ConvertToMessages(consumeResults);
                            await ProcessBatch(messages, cancellationToken);
                            
                            // Коммитим последнее сообщение в батче
                            var lastConsumeResult = consumeResults.Last();
                            _consumer.Commit(lastConsumeResult);
                            
                            _logger.LogDebug("Processed batch of {Count} messages", consumeResults.Count);
                        }
                        catch (Exception e)
                        {
                            _logger.LogError(e, "Error processing batch of {Count} messages", consumeResults.Count);
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Batch consumer cancelled");
            }
            catch (ConsumeException ex)
            {
                _logger.LogError(ex, "Batch consume error occurred");
            }
            finally
            {
                StopConsuming();
            }
        }
        
        private List<ConsumeResult<string, string>> CollectBatch(CancellationToken cancellationToken)
        {
            var batch = new List<ConsumeResult<string, string>>();
            var startTime = DateTime.UtcNow;
            
            while (batch.Count < _batchSize && !cancellationToken.IsCancellationRequested)
            {
                var timeLeft = _collectTimeoutMs - (int)(DateTime.UtcNow - startTime).TotalMilliseconds;
                if (timeLeft <= 0)
                {
                    break; // Таймаут
                }
                
                var consumeResult = _consumer.Consume(TimeSpan.FromMilliseconds(Math.Min(100, timeLeft)));
                
                if (consumeResult == null)
                {
                    continue; // Таймаут Consume
                }
                
                if (consumeResult.IsPartitionEOF)
                {
                    continue; // Достигнут конец партиции
                }
                
                if (consumeResult.Message != null)
                {
                    batch.Add(consumeResult);
                }
            }
            
            _logger.LogDebug("Collected batch of {Count} messages in {ElapsedMs}ms", 
                batch.Count, (DateTime.UtcNow - startTime).TotalMilliseconds);
            
            return batch;
        }
        
        private void StopConsuming()
        {
            _logger.LogInformation($"Stopping BATCH consuming from topic: {_topic}");
            _consumer.Close();
            _consumer.Dispose();
        }

        protected abstract Task ProcessBatch(List<Message<T>> messages, CancellationToken token);
        
        // Вспомогательный метод для преобразования ConsumeResult в Message<T>
        protected List<Message<T>> ConvertToMessages(List<ConsumeResult<string, string>> consumeResults)
        {
            return consumeResults.Select(cr => new Message<T>
            {
                Key = cr.Message.Key,
                Body = cr.Message.Value.FromJson<T>()
            }).ToList();
        }
    }
}