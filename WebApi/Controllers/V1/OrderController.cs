// FluentValidation
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
// BLL
using WebApi.BLL.Models;
using WebApi.BLL.Services;
using WebApi.Validators;
// DTO
using OrderDtoCommon = Models.Dto.Common;
using OrderDtoV1Requests = Models.Dto.V1.Requests;
using OrderDtoV1Responses = Models.Dto.V1.Responses;
using Microsoft.Extensions.Logging; // Добавляем для логирования

namespace WebApi.Controllers.V1
{
    [Route("api/v1/order")]
    [ApiController]
    public class OrderController : ControllerBase
    {
        private readonly OrderService _orderService;
        private readonly ValidatorFactory _validatorFactory;
        private readonly ILogger<OrderController> _logger; // Добавляем логгер

        // Внедряем ValidatorFactory вторым параметром
        public OrderController(
            OrderService orderService, 
            ValidatorFactory validatorFactory,
            ILogger<OrderController> logger) // Добавляем логгер в конструктор
        {
            _orderService = orderService;
            _validatorFactory = validatorFactory;
            _logger = logger;
        }

        [HttpPatch("update-status")]
        public async Task<ActionResult<OrderDtoV1Responses.V1UpdateOrdersStatusResponse>> UpdateOrdersStatus(
            [FromBody] OrderDtoV1Requests.V1UpdateOrdersStatusRequest request,
            CancellationToken token)
        {
            // Валидация запроса через FluentValidation
            var validationResult = await _validatorFactory
                .GetValidator<OrderDtoV1Requests.V1UpdateOrdersStatusRequest>()
                .ValidateAsync(request, token);

            if (!validationResult.IsValid)
                return BadRequest(validationResult.ToDictionary());
        
            try
            {
                // Вызываем сервис для обновления статусов
                await _orderService.UpdateOrdersStatus(request.OrderIds, request.NewStatus, token);
                
                _logger.LogInformation(
                    "Successfully updated status to '{NewStatus}' for orders: {OrderIds}",
                    request.NewStatus,
                    string.Join(", ", request.OrderIds));
                
                // Возвращаем пустой ответ со статусом 200 (как указано в задании)
                return Ok(new OrderDtoV1Responses.V1UpdateOrdersStatusResponse());
            }
            catch (ArgumentException ex)
            {
                // Невалидный статус
                _logger.LogWarning(ex, "Invalid status argument for orders: {OrderIds}", 
                    string.Join(", ", request.OrderIds));
                return BadRequest(new { Error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                // Невалидный переход статуса (например, "created" → "completed")
                _logger.LogWarning(ex, "Invalid status transition for orders: {OrderIds}", 
                    string.Join(", ", request.OrderIds));
                return BadRequest(new { Error = ex.Message });
            }
            catch (Exception ex)
            {
                // Другие ошибки
                _logger.LogError(ex, "Error updating orders status for orderIds: {OrderIds}", 
                    string.Join(", ", request.OrderIds));
                return StatusCode(500, new { Error = "Internal server error" });
            }
        }

        [HttpPost("batch-create")]
        public async Task<ActionResult<OrderDtoV1Responses.V1CreateOrderResponse>> V1BatchCreate(
            [FromBody] OrderDtoV1Requests.V1CreateOrderRequest request,
            CancellationToken token)
        {
            // Валидация запроса через FluentValidation
            var validationResult = await _validatorFactory
                .GetValidator<OrderDtoV1Requests.V1CreateOrderRequest>()
                .ValidateAsync(request, token);

            if (!validationResult.IsValid)
                return BadRequest(validationResult.ToDictionary());

            if (request?.Orders == null || request.Orders.Length == 0)
                return BadRequest("Orders cannot be null or empty.");

            var ordersBll = request.Orders.Select(x => new OrderUnit
            {
                CustomerId = x.CustomerId,
                DeliveryAddress = x.DeliveryAddress,
                TotalPriceCents = x.TotalPriceCents,
                TotalPriceCurrency = x.TotalPriceCurrency,
                OrderItems = x.OrderItems?.Select(p => new OrderItemUnit
                {
                    ProductId = p.ProductId,
                    Quantity = p.Quantity,
                    ProductTitle = p.ProductTitle,
                    ProductUrl = p.ProductUrl,
                    PriceCents = p.PriceCents,
                    PriceCurrency = p.PriceCurrency
                }).ToArray() ?? Array.Empty<OrderItemUnit>()
            }).ToArray();

            var res = await _orderService.BatchInsert(ordersBll, token);

            return Ok(new OrderDtoV1Responses.V1CreateOrderResponse
            {
                Orders = MapToDto(res)
            });
        }

        [HttpPost("query")]
        public async Task<ActionResult<OrderDtoV1Responses.V1QueryOrdersResponse>> V1QueryOrders(
            [FromBody] OrderDtoV1Requests.V1QueryOrdersRequest request,
            CancellationToken token)
        {
            // Валидация запроса через FluentValidation
            var validationResult = await _validatorFactory
                .GetValidator<OrderDtoV1Requests.V1QueryOrdersRequest>()
                .ValidateAsync(request, token);

            if (!validationResult.IsValid)
                return BadRequest(validationResult.ToDictionary());

            var queryModel = new WebApi.BLL.Models.QueryOrderItemsModel
            {
                Ids = request.Ids,                 // int[]? или List<int>?
                CustomerIds = request.CustomerIds, // int[]? или List<int>?
                Page = request.Page,               // int
                PageSize = request.PageSize,       // int
                IncludeOrderItems = request.IncludeOrderItems // bool
            };

            var res = await _orderService.GetOrders(queryModel, token);

            return Ok(new OrderDtoV1Responses.V1QueryOrdersResponse
            {
                Orders = MapToDto(res)
            });
        }

        private OrderDtoCommon.OrderUnit[] MapToDto(OrderUnit[] orders)
        {
            if (orders == null || orders.Length == 0)
                return Array.Empty<OrderDtoCommon.OrderUnit>();

            return orders.Select(x => new OrderDtoCommon.OrderUnit
            {
                Id = x.Id,
                CustomerId = x.CustomerId,
                DeliveryAddress = x.DeliveryAddress,
                TotalPriceCents = x.TotalPriceCents,
                TotalPriceCurrency = x.TotalPriceCurrency,
                Status = x.Status, // Добавляем статус в DTO
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt,
                OrderItems = x.OrderItems?.Select(p => new OrderDtoCommon.OrderItemUnit
                {
                    Id = p.Id,
                    OrderId = p.OrderId,
                    ProductId = p.ProductId,
                    Quantity = p.Quantity,
                    ProductTitle = p.ProductTitle,
                    ProductUrl = p.ProductUrl,
                    PriceCents = p.PriceCents,
                    PriceCurrency = p.PriceCurrency,
                    CreatedAt = p.CreatedAt,
                    UpdatedAt = p.UpdatedAt
                }).ToArray() ?? Array.Empty<OrderDtoCommon.OrderItemUnit>()
            }).ToArray();
        }
    }
}