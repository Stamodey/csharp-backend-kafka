using Consumer.Base;
using Consumer.Clients;
using Consumer.Config;
using Messages;
using Microsoft.Extensions.Options;
using Models.Dto.V1.Requests;

namespace Consumer.Consumers
{
    public class OmsOrderStatusChangedConsumer : BaseKafkaConsumer<OmsOrderStatusChangedMessage>
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<OmsOrderStatusChangedConsumer> _logger;
        
        public OmsOrderStatusChangedConsumer(
            IOptions<KafkaSettings> kafkaSettings,
            IServiceProvider serviceProvider,
            ILogger<OmsOrderStatusChangedConsumer> logger)
            : base(kafkaSettings, kafkaSettings.Value.OmsOrderStatusChangedTopic, logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ProcessMessage(Message<OmsOrderStatusChangedMessage> message, CancellationToken token)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            try
            {
                _logger.LogInformation(
                    "Processing order status changed: OrderId={OrderId}, Status={Status}, Old={OldStatus}, New={NewStatus}, Key={Key}", 
                    message.Body.OrderId, message.Body.Status, message.Body.OldStatus, message.Body.NewStatus, message.Key);
                
                // Пример: логирование изменения статуса через API
                using var scope = _serviceProvider.CreateScope();
                var client = scope.ServiceProvider.GetRequiredService<OmsClient>();

                await client.LogOrder(new V1AuditLogOrderRequest
                {
                    Orders = new[]
                    {
                        new V1AuditLogOrderRequest.LogOrder
                        {
                            OrderId = message.Body.OrderId,
                            OrderItemId = 0, // Или получите OrderItemId из БД если нужно
                            CustomerId = message.Body.CustomerId,
                            OrderStatus = message.Body.NewStatus
                        }
                    }
                }, token);

                stopwatch.Stop();
                _logger.LogInformation("Order status changed processed in {ElapsedMs} ms", stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, 
                    "Error processing order status changed message: OrderId={OrderId}, Key={Key}", 
                    message.Body.OrderId, message.Key);
                throw;
            }
        }
    }
}