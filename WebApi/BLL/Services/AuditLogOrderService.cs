using Models.Dto.V1.Requests;
using Models.Dto.V1.Responses;
using WebApi.DAL;
using WebApi.DAL.Interfaces;
using WebApi.DAL.Models;

namespace WebApi.BLL.Services
{
    public interface IAuditLogOrderService
    {
        Task ProcessAuditLogAsync(V1AuditLogOrderRequest request);
        Task<IEnumerable<V1AuditLogOrderResponse>> GetAuditLogsByOrderIdAsync(long orderId);
        Task<IEnumerable<V1AuditLogOrderResponse>> GetAuditLogsByCustomerIdAsync(long customerId);
    }

    public class AuditLogOrderService : IAuditLogOrderService
    {
        private readonly UnitOfWork _unitOfWork;
        private readonly IAuditLogOrderRepository _auditLogOrderRepository;

        public AuditLogOrderService(UnitOfWork unitOfWork, IAuditLogOrderRepository auditLogOrderRepository)
        {
            _unitOfWork = unitOfWork;
            _auditLogOrderRepository = auditLogOrderRepository;
        }

        public async Task ProcessAuditLogAsync(V1AuditLogOrderRequest request)
        {
            var auditLogs = request.Orders.Select(order => new V1AuditLogOrderDal
            {
                OrderId = order.OrderId,
                OrderItemId = order.OrderItemId,
                CustomerId = order.CustomerId,
                OrderStatus = order.OrderStatus
            });

            // Асинхронная запись без блокировки
            await _auditLogOrderRepository.AddBatchAsync(auditLogs);
        }

        public async Task<IEnumerable<V1AuditLogOrderResponse>> GetAuditLogsByOrderIdAsync(long orderId)
        {
            var logs = await _auditLogOrderRepository.GetByOrderIdAsync(orderId);

            return logs.Select(log => new V1AuditLogOrderResponse
            {
                Id = log.Id,
                OrderId = log.OrderId,
                OrderItemId = log.OrderItemId,
                CustomerId = log.CustomerId,
                OrderStatus = log.OrderStatus,
                CreatedAt = log.CreatedAt
            });
        }

        public async Task<IEnumerable<V1AuditLogOrderResponse>> GetAuditLogsByCustomerIdAsync(long customerId)
        {
            var logs = await _auditLogOrderRepository.GetByCustomerIdAsync(customerId);

            return logs.Select(log => new V1AuditLogOrderResponse
            {
                Id = log.Id,
                OrderId = log.OrderId,
                OrderItemId = log.OrderItemId,
                CustomerId = log.CustomerId,
                OrderStatus = log.OrderStatus,
                CreatedAt = log.CreatedAt
            });
        }
    }
}