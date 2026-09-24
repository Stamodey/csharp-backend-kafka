using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Models.Dto.V1.Requests;
using WebApi.BLL.Services;

namespace WebApi.Controllers.V1
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class AuditLogOrderController : ControllerBase
    {
        private readonly IAuditLogOrderService _auditLogService;
        private readonly IValidator<V1AuditLogOrderRequest> _validator;
        private readonly ILogger<AuditLogOrderController> _logger;

        public AuditLogOrderController(
            IAuditLogOrderService auditLogService,
            IValidator<V1AuditLogOrderRequest> validator,
            ILogger<AuditLogOrderController> logger)
        {
            _auditLogService = auditLogService;
            _validator = validator;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> CreateAuditLog([FromBody] V1AuditLogOrderRequest request)
        {
            // Валидация запроса
            var validationResult = await _validator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                return BadRequest(validationResult.Errors);
            }

            // Асинхронная обработка без ожидания - fire and forget
            _ = Task.Run(async () =>
            {
                try
                {
                    await _auditLogService.ProcessAuditLogAsync(request);
                    _logger.LogInformation("Audit log processed successfully for orders: {OrderIds}", 
                        string.Join(", ", request.Orders.Select(o => o.OrderId)));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing audit log in background task for orders: {OrderIds}", 
                        string.Join(", ", request.Orders.Select(o => o.OrderId)));
                }
            });

            // Немедленный ответ клиенту
            return Accepted(new
            {
                Message = "Audit log is being processed asynchronously",
                OrdersCount = request.Orders.Length,
                ProcessedAt = DateTime.UtcNow
            });
        }

        [HttpGet("order/{orderId:long}")]
        public async Task<IActionResult> GetAuditLogsByOrderId(long orderId)
        {
            if (orderId <= 0)
                return BadRequest(new { Error = "OrderId must be greater than 0" });

            var logs = await _auditLogService.GetAuditLogsByOrderIdAsync(orderId);
            return Ok(new { OrderId = orderId, AuditLogs = logs });
        }

        [HttpGet("customer/{customerId:long}")]
        public async Task<IActionResult> GetAuditLogsByCustomerId(long customerId)
        {
            if (customerId <= 0)
                return BadRequest(new { Error = "CustomerId must be greater than 0" });

            var logs = await _auditLogService.GetAuditLogsByCustomerIdAsync(customerId);
            return Ok(new { CustomerId = customerId, AuditLogs = logs });
        }
    }
}