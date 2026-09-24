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
    public class BatchOmsOrderStatusChangedConsumer : BaseBatchKafkaConsumer<OmsOrderStatusChangedMessage>
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<BatchOmsOrderStatusChangedConsumer> _logger;
        
        public BatchOmsOrderStatusChangedConsumer(
            IOptions<KafkaSettings> kafkaSettings,
            IServiceProvider serviceProvider,
            ILogger<BatchOmsOrderStatusChangedConsumer> logger)
            : base(kafkaSettings, kafkaSettings.Value.OmsOrderStatusChangedTopic, logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ProcessBatch(List<Message<OmsOrderStatusChangedMessage>> messages, CancellationToken token)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            try
            {
                _logger.LogInformation(
                    "Processing BATCH of {Count} order status changed messages", 
                    messages.Count);
                
                // Пример: логирование изменения статусов через API БАТЧЕМ
                using var scope = _serviceProvider.CreateScope();
                var client = scope.ServiceProvider.GetRequiredService<OmsClient>();

                // Собираем все изменения статусов в один запрос
                var allOrders = messages.Select(message => 
                    new V1AuditLogOrderRequest.LogOrder
                    {
                        OrderId = message.Body.OrderId,
                        OrderItemId = 0, // Или получите из БД если нужно
                        CustomerId = message.Body.CustomerId,
                        OrderStatus = message.Body.NewStatus
                    }).ToArray();

                if (allOrders.Any())
                {
                    await client.LogOrder(new V1AuditLogOrderRequest
                    {
                        Orders = allOrders
                    }, token);
                }

                stopwatch.Stop();
                _logger.LogInformation("Batch of {Count} order status changes processed in {ElapsedMs} ms", 
                    messages.Count, stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, 
                    "Error processing batch of order status changed messages. Count: {Count}", 
                    messages.Count);
                throw;
            }
        }
    }
}