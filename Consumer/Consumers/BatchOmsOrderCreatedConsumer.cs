using Common;
using Consumer.Base;
using Consumer.Clients;
using Consumer.Config;
using Messages;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Models.Dto.V1.Requests;

namespace Consumer.Consumers
{
    public class BatchOmsOrderCreatedConsumer : BaseBatchKafkaConsumer<OmsOrderCreatedMessage>
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<BatchOmsOrderCreatedConsumer> _logger;
        
        public BatchOmsOrderCreatedConsumer(
            IOptions<KafkaSettings> kafkaSettings,
            IServiceProvider serviceProvider,
            ILogger<BatchOmsOrderCreatedConsumer> logger)
            : base(kafkaSettings, kafkaSettings.Value.OmsOrderCreatedTopic, logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ProcessBatch(List<Message<OmsOrderCreatedMessage>> messages, CancellationToken token)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            try
            {
                _logger.LogInformation(
                    "Processing BATCH of {Count} order created messages", 
                    messages.Count);
                
                using var scope = _serviceProvider.CreateScope();
                var client = scope.ServiceProvider.GetRequiredService<OmsClient>();
                
                var allOrders = new List<V1AuditLogOrderRequest.LogOrder>();
                
                foreach (var message in messages)
                {
                    allOrders.AddRange(message.Body.OrderItems.Select(x =>
                        new V1AuditLogOrderRequest.LogOrder
                        {
                            OrderId = message.Body.Id,
                            OrderItemId = x.Id,
                            CustomerId = message.Body.CustomerId,
                            OrderStatus = message.Body.Status.ToString()
                        }));
                }

                if (allOrders.Any())
                {
                    await client.LogOrder(new V1AuditLogOrderRequest
                    {
                        Orders = allOrders.ToArray()
                    }, token);
                }

                stopwatch.Stop();
                _logger.LogInformation("Batch of {Count} orders created processed in {ElapsedMs} ms", 
                    messages.Count, stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, 
                    "Error processing batch of order created messages. Count: {Count}", 
                    messages.Count);
                throw;
            }
        }
    }
}