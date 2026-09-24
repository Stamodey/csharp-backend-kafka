using WebApi.BLL.Models;
using WebApi.DAL;
using WebApi.DAL.Interfaces;
using WebApi.DAL.Models;
using Microsoft.Extensions.Options;
using WebApi.Config;
using Messages;

namespace WebApi.BLL.Services
{
    public class OrderService
    {
        private readonly UnitOfWork _unitOfWork;
        private readonly IOrderRepository _orderRepository;
        private readonly IOrderItemRepository _orderItemRepository;
        private readonly KafkaProducer _kafkaProducer;
        private readonly IOptions<KafkaSettings> _kafkaSettings;

        public OrderService(
            UnitOfWork unitOfWork,
            IOrderRepository orderRepository,
            IOrderItemRepository orderItemRepository,
            KafkaProducer kafkaProducer,
            IOptions<KafkaSettings> kafkaSettings)
        {
            _unitOfWork = unitOfWork;
            _orderRepository = orderRepository;
            _orderItemRepository = orderItemRepository;
            _kafkaProducer = kafkaProducer;
            _kafkaSettings = kafkaSettings;
        }

        /// <summary>
        /// Метод обновления статусов заказов
        /// </summary>
        public async Task UpdateOrdersStatus(long[] orderIds, string newStatus, CancellationToken token)
        {
            if (orderIds == null || orderIds.Length == 0)
                return;
                
            // Получаем текущие статусы заказов
            var currentStatuses = await _orderRepository.GetOrdersStatus(orderIds, token);
            
            // Если заказы не найдены - просто возвращаемся (как указано в задании)
            if (currentStatuses.Count == 0)
                return;
                
            // Проверяем валидность переходов для каждого заказа
            foreach (var orderStatus in currentStatuses)
            {
                OrderStatusService.ValidateTransition(orderStatus.Value, newStatus);
            }
            
            // Обновляем статусы в базе данных
            await using var transaction = await _unitOfWork.BeginTransactionAsync(token);
            
            try
            {
                var updatedCount = await _orderRepository.UpdateOrdersStatus(orderIds, newStatus, token);
                await transaction.CommitAsync(token);
                
                // Публикуем события об изменении статусов
                await PublishOrderStatusChangedEvents(currentStatuses, newStatus, token);
            }
            catch
            {
                await transaction.RollbackAsync(token);
                throw;
            }
        }

        /// <summary>
        /// Метод создания заказов
        /// </summary>
        public async Task<OrderUnit[]> BatchInsert(OrderUnit[] orderUnits, CancellationToken token)
        {
            var now = DateTimeOffset.UtcNow;
            await using var transaction = await _unitOfWork.BeginTransactionAsync(token);

            try
            {
                // Мапим OrderUnit -> V1OrderDal
                var orderModels = orderUnits.Select(x => new V1OrderDal
                {
                    CustomerId = x.CustomerId,
                    DeliveryAddress = x.DeliveryAddress,
                    TotalPriceCents = x.TotalPriceCents,
                    TotalPriceCurrency = x.TotalPriceCurrency,
                    Status = OrderStatusService.Created, // Устанавливаем статус по умолчанию
                    CreatedAt = now,
                    UpdatedAt = now
                }).ToArray();

                // Вставляем заказы
                var insertedOrders = await _orderRepository.BulkInsert(orderModels, token);

                // Мапим OrderItemUnit -> V1OrderItemDal с правильным OrderId
                var orderItemModels = orderUnits
                    .SelectMany((ou, i) => ou.OrderItems.Select(oi => new V1OrderItemDal
                    {
                        OrderId = insertedOrders[i].Id,
                        ProductId = oi.ProductId,
                        Quantity = oi.Quantity,
                        ProductTitle = oi.ProductTitle,
                        ProductUrl = oi.ProductUrl,
                        PriceCents = oi.PriceCents,
                        PriceCurrency = oi.PriceCurrency,
                        CreatedAt = now,
                        UpdatedAt = now
                    }))
                    .ToArray();

                // Вставляем позиции
                var insertedItems = await _orderItemRepository.BulkInsert(orderItemModels, token);

                await transaction.CommitAsync(token);

                // Публикуем события о создании заказов
                await PublishOrderCreatedEvents(insertedOrders, insertedItems, token);

                // Создаём lookup по OrderId для маппинга
                var lookup = insertedItems.ToLookup(x => x.OrderId);
                return Map(insertedOrders, lookup);
            }
            catch
            {
                await transaction.RollbackAsync(token);
                throw;
            }
        }

        /// <summary>
        /// Публикация событий о создании заказов в Kafka
        /// </summary>
        private async Task PublishOrderCreatedEvents(V1OrderDal[] orders, V1OrderItemDal[] orderItems, CancellationToken token)
        {
            var lookup = orderItems.ToLookup(x => x.OrderId);
            var messages = orders.Select(order => new OmsOrderCreatedMessage
            {
                Id = order.Id,
                CustomerId = order.CustomerId,
                DeliveryAddress = order.DeliveryAddress,
                TotalPriceCents = order.TotalPriceCents,
                TotalPriceCurrency = order.TotalPriceCurrency,
                Status = MapStatusToEnum(order.Status),
                CreatedAt = order.CreatedAt,
                UpdatedAt = order.UpdatedAt,
                OrderItems = lookup[order.Id].Select(item => new OrderItemDto
                {
                    Id = item.Id,
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    ProductTitle = item.ProductTitle,
                    ProductUrl = item.ProductUrl,
                    PriceCents = item.PriceCents,
                    PriceCurrency = item.PriceCurrency
                }).ToList()
            }).ToArray();

            // Отправляем в Kafka вместо RabbitMQ
            // Ключом будет CustomerId, чтобы сообщения одного пользователя попадали в одну партицию
            var kafkaMessages = messages.Select(m => (m.CustomerId.ToString(), m)).ToArray();
            
            await _kafkaProducer.Produce(
                _kafkaSettings.Value.OmsOrderCreatedTopic,
                kafkaMessages,
                token
            );
        }

        /// <summary>
        /// Публикация событий об изменении статусов заказов в Kafka
        /// </summary>
        private async Task PublishOrderStatusChangedEvents(
            Dictionary<long, string> oldStatuses, 
            string newStatus, 
            CancellationToken token)
        {
            // Получаем CustomerId для каждого заказа, чтобы использовать как ключ
            var orderIds = oldStatuses.Keys.ToArray();
            var orders = await _orderRepository.GetOrdersByIds(orderIds, token);
            var orderLookup = orders.ToDictionary(o => o.Id);

            var messages = oldStatuses.Select(kvp => 
            {
                return new OmsOrderStatusChangedMessage
                {
                    OrderId = kvp.Key,
                    Status = MapStatusToEnum(newStatus),
                    ChangedAt = DateTimeOffset.UtcNow,
                    OldStatus = kvp.Value,
                    NewStatus = newStatus
                };
            }).ToArray();

            // Отправляем в Kafka
            // Ключом будет CustomerId, если его нет - используем OrderId как fallback
            var kafkaMessages = messages.Select(m => 
            {
                var order = orderLookup.ContainsKey(m.OrderId) ? orderLookup[m.OrderId] : null;
                var key = order?.CustomerId.ToString() ?? m.OrderId.ToString();
                return (key, m);
            }).ToArray();
            
            await _kafkaProducer.Produce(
                _kafkaSettings.Value.OmsOrderStatusChangedTopic,
                kafkaMessages,
                token
            );
        }

        /// <summary>
        /// Преобразует строковый статус в enum OrderStatus
        /// </summary>
        private OrderStatus MapStatusToEnum(string status)
        {
            if (string.IsNullOrEmpty(status))
                return OrderStatus.Created;
            
            return status.ToLower() switch
            {
                "created" => OrderStatus.Created,
                "processing" => OrderStatus.Processing,
                "completed" => OrderStatus.Completed,
                "cancelled" => OrderStatus.Cancelled,
                _ => OrderStatus.Created
            };
        }

        /// <summary>
        /// Метод получения заказов
        /// </summary>
        public async Task<OrderUnit[]> GetOrders(QueryOrderItemsModel model, CancellationToken token)
        {
            var orders = await _orderRepository.Query(new QueryOrdersDalModel
            {
                Ids = model.Ids,
                CustomerIds = model.CustomerIds,
                Limit = model.PageSize,
                Offset = (model.Page - 1) * model.PageSize
            }, token);

            if (orders.Length == 0)
            {
                return Array.Empty<OrderUnit>();
            }

            ILookup<long, V1OrderItemDal> orderItemLookup = null;
            if (model.IncludeOrderItems)
            {
                var orderItems = await _orderItemRepository.Query(new QueryOrderItemsDalModel
                {
                    OrderIds = orders.Select(x => x.Id).ToArray(),
                }, token);

                orderItemLookup = orderItems.ToLookup(x => x.OrderId);
            }

            return Map(orders, orderItemLookup);
        }

        private OrderUnit[] Map(V1OrderDal[] orders, ILookup<long, V1OrderItemDal> orderItemLookup = null)
        {
            return orders.Select(x => new OrderUnit
            {
                Id = x.Id,
                CustomerId = x.CustomerId,
                DeliveryAddress = x.DeliveryAddress,
                TotalPriceCents = x.TotalPriceCents,
                TotalPriceCurrency = x.TotalPriceCurrency,
                Status = x.Status,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt,
                OrderItems = orderItemLookup?[x.Id].Select(o => new WebApi.BLL.Models.OrderItemUnit
                {
                    Id = o.Id,
                    OrderId = o.OrderId,
                    ProductId = o.ProductId,
                    Quantity = o.Quantity,
                    ProductTitle = o.ProductTitle,
                    ProductUrl = o.ProductUrl,
                    PriceCents = o.PriceCents,
                    PriceCurrency = o.PriceCurrency,
                    CreatedAt = o.CreatedAt,
                    UpdatedAt = o.UpdatedAt
                }).ToArray() ?? Array.Empty<WebApi.BLL.Models.OrderItemUnit>()
            }).ToArray();
        }
    }
}