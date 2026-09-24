using Consumer.Base;
using Consumer.Clients;
using Consumer.Config;
using Messages;
using Microsoft.Extensions.Options;
using Models.Dto.V1.Requests;

namespace Consumer.Consumers
{
    public class OmsOrderCreatedConsumer : BaseKafkaConsumer<OmsOrderCreatedMessage>
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<OmsOrderCreatedConsumer> _logger;
        
        public OmsOrderCreatedConsumer(
            IOptions<KafkaSettings> kafkaSettings,
            IServiceProvider serviceProvider,
            ILogger<OmsOrderCreatedConsumer> logger)
            : base(kafkaSettings, kafkaSettings.Value.OmsOrderCreatedTopic, logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ProcessMessage(Message<OmsOrderCreatedMessage> message, CancellationToken token)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            try
            {
                _logger.LogInformation(
                    "Processing order created: OrderId={OrderId}, CustomerId={CustomerId}, Key={Key}, PartitionKey={PartitionKey}", 
                    message.Body.Id, message.Body.CustomerId, message.Key, message.Key);
                
                // Пример: логирование в БД через API
                using var scope = _serviceProvider.CreateScope();
                var client = scope.ServiceProvider.GetRequiredService<OmsClient>();

                await client.LogOrder(new V1AuditLogOrderRequest
                {
                    Orders = message.Body.OrderItems.Select(x =>
                        new V1AuditLogOrderRequest.LogOrder
                        {
                            OrderId = message.Body.Id,
                            OrderItemId = x.Id,
                            CustomerId = message.Body.CustomerId,
                            OrderStatus = message.Body.Status.ToString()
                        }).ToArray()
                }, token);

                stopwatch.Stop();
                _logger.LogInformation("Order created processed in {ElapsedMs} ms", stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, 
                    "Error processing order created message: OrderId={OrderId}, Key={Key}", 
                    message.Body.Id, message.Key);
                throw; // Бросим исключение, чтобы сообщение не было закоммичено и было обработано повторно
            }
        }
    }
}