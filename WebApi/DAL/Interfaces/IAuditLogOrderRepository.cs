using Npgsql;
using WebApi.DAL.Models;

namespace WebApi.DAL.Interfaces
{
    public interface IAuditLogOrderRepository
    {
        Task<V1AuditLogOrderDal[]> BulkInsert(V1AuditLogOrderDal[] model, CancellationToken token);
        Task<V1AuditLogOrderDal[]> GetByOrderIdAsync(long orderId, CancellationToken token);
        Task<V1AuditLogOrderDal[]> GetByCustomerIdAsync(long customerId, CancellationToken token);
        Task<V1AuditLogOrderDal[]> Query(QueryAuditLogOrderDalModel model, CancellationToken token);
        
        // Опционально - если нужно сохранить старые методы для обратной совместимости
        Task AddBatchAsync(IEnumerable<V1AuditLogOrderDal> auditLogs, NpgsqlTransaction transaction = null);
        Task<IEnumerable<V1AuditLogOrderDal>> GetByOrderIdAsync(long orderId);
        Task<IEnumerable<V1AuditLogOrderDal>> GetByCustomerIdAsync(long customerId);
    }
}