/*using AutoFixture;
using WebApi.BLL.Models;
using WebApi.BLL.Services;
using Microsoft.Extensions.Logging;

namespace WebApi.Jobs
{
    public class OrderGenerator(IServiceProvider serviceProvider, ILogger<OrderGenerator> logger) : BackgroundService
    {
        private readonly Random _random = new Random();
        
        // Пул из 5 CustomerId для тестирования
        private readonly long[] _customerIds = { 1, 2, 3, 4, 5};
        
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var fixture = new Fixture();
            using var scope = serviceProvider.CreateScope();
            var orderService = scope.ServiceProvider.GetRequiredService<OrderService>();

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var orders = Enumerable.Range(1, 50)
                        .Select(_ =>
                        {
                            var orderItem = fixture.Build<OrderItemUnit>()
                                .With(x => x.PriceCurrency, "RUB")
                                .With(x => x.PriceCents, 1000)
                                .Create();

                            var randomCustomerId = _customerIds[_random.Next(_customerIds.Length)];
                            
                            var order = fixture.Build<OrderUnit>()
                                .With(x => x.CustomerId, randomCustomerId) // Используем CustomerId из пула
                                .With(x => x.TotalPriceCurrency, "RUB")
                                .With(x => x.TotalPriceCents, 1000)
                                .With(x => x.OrderItems, new[] { orderItem })
                                .Create();

                            return order;
                        })
                        .ToArray();
                    
                    var createdOrders = await orderService.BatchInsert(orders, stoppingToken);
                    
                    logger.LogInformation("Created {Count} orders", createdOrders.Length);
                    
                    if (createdOrders.Length > 0)
                    {
                        // Логируем распределение по CustomerId для отладки
                        var customerDistribution = createdOrders
                            .GroupBy(o => o.CustomerId)
                            .Select(g => new { CustomerId = g.Key, Count = g.Count() })
                            .ToList();
                        
                        logger.LogInformation("Customer distribution: {Distribution}", 
                            string.Join(", ", customerDistribution.Select(x => $"Customer{x.CustomerId}: {x.Count}")));
                        
                        // 2. ВСЕ заказы -> "processing" (это разрешено)
                        var allOrderIds = createdOrders.Select(o => o.Id).ToArray();
                        logger.LogInformation("Updating ALL {Count} orders to 'processing'", allOrderIds.Length);
                        await orderService.UpdateOrdersStatus(allOrderIds, "processing", stoppingToken);
                        
                        // 3. СЛУЧАЙНОЕ подмножество -> "completed" или "cancelled"
                        var ordersForSecondUpdate = GetRandomOrders(createdOrders, _random.Next(1, createdOrders.Length));
                        var secondOrderIds = ordersForSecondUpdate.Select(o => o.Id).ToArray();
                        
                        var finalStatus = _random.Next(2) == 0 ? "completed" : "cancelled";
                        logger.LogInformation("Updating {Count} orders to '{Status}'", secondOrderIds.Length, finalStatus);
                        await orderService.UpdateOrdersStatus(secondOrderIds, finalStatus, stoppingToken);
                        
                        // 4. ЕЩЁ более случайное подмножество -> другой финальный статус (если остались)
                        var remainingOrders = createdOrders
                            .Where(o => !secondOrderIds.Contains(o.Id))
                            .ToArray();
                            
                        if (remainingOrders.Length > 0)
                        {
                            var thirdOrderIds = GetRandomOrders(remainingOrders, _random.Next(1, remainingOrders.Length + 1))
                                .Select(o => o.Id)
                                .ToArray();
                                
                            var thirdStatus = finalStatus == "completed" ? "cancelled" : "completed";
                            logger.LogInformation("Updating {Count} more orders to '{Status}'", thirdOrderIds.Length, thirdStatus);
                            await orderService.UpdateOrdersStatus(thirdOrderIds, thirdStatus, stoppingToken);
                        }
                    }
                    
                    var delay = _random.Next(500, 3000);
                    await Task.Delay(delay, stoppingToken);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error in OrderGenerator");
                    await Task.Delay(5000, stoppingToken);
                }
            }
        }

        private OrderUnit[] GetRandomOrders(OrderUnit[] orders, int count)
        {
            return orders
                .OrderBy(_ => _random.Next())
                .Take(count)
                .ToArray();
        }
    }
}*/
using AutoFixture;
using WebApi.BLL.Models;
using WebApi.BLL.Services;
using Microsoft.Extensions.Logging;

namespace WebApi.Jobs
{
    public class OrderGenerator(IServiceProvider serviceProvider, ILogger<OrderGenerator> logger) : BackgroundService
    {
        private readonly Random _random = new Random();
        
        // Пул из 5 CustomerId для тестирования
        private readonly long[] _customerIds = { 1, 2, 3, 4, 5};
        
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var fixture = new Fixture();
            using var scope = serviceProvider.CreateScope();
            var orderService = scope.ServiceProvider.GetRequiredService<OrderService>();

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // ИЗМЕНЕНО: 1000 заказов вместо 50
                    var orders = Enumerable.Range(1, 1000)  // ← ИЗМЕНЕНО ЗДЕСЬ: 50 → 1000
                        .Select(_ =>
                        {
                            var orderItem = fixture.Build<OrderItemUnit>()
                                .With(x => x.PriceCurrency, "RUB")
                                .With(x => x.PriceCents, 1000)
                                .Create();

                            var randomCustomerId = _customerIds[_random.Next(_customerIds.Length)];
                            
                            var order = fixture.Build<OrderUnit>()
                                .With(x => x.CustomerId, randomCustomerId) // Используем CustomerId из пула
                                .With(x => x.TotalPriceCurrency, "RUB")
                                .With(x => x.TotalPriceCents, 1000)
                                .With(x => x.OrderItems, new[] { orderItem })
                                .Create();

                            return order;
                        })
                        .ToArray();
                    
                    var createdOrders = await orderService.BatchInsert(orders, stoppingToken);
                    
                    logger.LogInformation("Created {Count} orders", createdOrders.Length);
                    
                    if (createdOrders.Length > 0)
                    {
                        // Логируем распределение по CustomerId для отладки
                        var customerDistribution = createdOrders
                            .GroupBy(o => o.CustomerId)
                            .Select(g => new { CustomerId = g.Key, Count = g.Count() })
                            .ToList();
                        
                        logger.LogInformation("Customer distribution: {Distribution}", 
                            string.Join(", ", customerDistribution.Select(x => $"Customer{x.CustomerId}: {x.Count}")));
                        
                        // 2. ВСЕ заказы -> "processing" (это разрешено)
                        // ИЗМЕНЕНО: БЕЗ ЗАДЕРЖКИ 100 мс
                        var allOrderIds = createdOrders.Select(o => o.Id).ToArray();
                        logger.LogInformation("Updating ALL {Count} orders to 'processing'", allOrderIds.Length);
                        await orderService.UpdateOrdersStatus(allOrderIds, "processing", stoppingToken);
                        
                        // 3. СЛУЧАЙНОЕ подмножество -> "completed" или "cancelled"
                        var ordersForSecondUpdate = GetRandomOrders(createdOrders, _random.Next(1, createdOrders.Length));
                        var secondOrderIds = ordersForSecondUpdate.Select(o => o.Id).ToArray();
                        
                        var finalStatus = _random.Next(2) == 0 ? "completed" : "cancelled";
                        logger.LogInformation("Updating {Count} orders to '{Status}'", secondOrderIds.Length, finalStatus);
                        await orderService.UpdateOrdersStatus(secondOrderIds, finalStatus, stoppingToken);
                        
                        // 4. ЕЩЁ более случайное подмножество -> другой финальный статус (если остались)
                        var remainingOrders = createdOrders
                            .Where(o => !secondOrderIds.Contains(o.Id))
                            .ToArray();
                            
                        if (remainingOrders.Length > 0)
                        {
                            var thirdOrderIds = GetRandomOrders(remainingOrders, _random.Next(1, remainingOrders.Length + 1))
                                .Select(o => o.Id)
                                .ToArray();
                                
                            var thirdStatus = finalStatus == "completed" ? "cancelled" : "completed";
                            logger.LogInformation("Updating {Count} more orders to '{Status}'", thirdOrderIds.Length, thirdStatus);
                            await orderService.UpdateOrdersStatus(thirdOrderIds, thirdStatus, stoppingToken);
                        }
                    }
                    
                    // ИЗМЕНЕНО: Убрана случайная задержка 500-3000 мс, 
                    // чтобы сразу идти на второй круг без задержки
                    // УБРАНО: var delay = _random.Next(500, 3000);
                    // УБРАНО: await Task.Delay(delay, stoppingToken);
                    // 
                    // Теперь цикл сразу идет на второй круг без задержки
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error in OrderGenerator");
                    // Оставляем задержку только при ошибках
                    await Task.Delay(5000, stoppingToken);
                }
            }
        }

        private OrderUnit[] GetRandomOrders(OrderUnit[] orders, int count)
        {
            return orders
                .OrderBy(_ => _random.Next())
                .Take(count)
                .ToArray();
        }
    }
}