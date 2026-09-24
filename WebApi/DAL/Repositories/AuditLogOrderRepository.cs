using Dapper;
using System.Text;
using Npgsql;
using WebApi.DAL.Interfaces;
using WebApi.DAL.Models;

namespace WebApi.DAL.Repositories
{
    public class AuditLogOrderRepository(UnitOfWork unitOfWork) : IAuditLogOrderRepository
    {
        public async Task<V1AuditLogOrderDal[]> BulkInsert(V1AuditLogOrderDal[] model, CancellationToken token)
        {
            var sql = @"
            INSERT INTO audit_log_order
            (
                order_id,
                order_item_id,
                customer_id,
                order_status,
                created_at,
                updated_at
            )
            SELECT 
                order_id,
                order_item_id,
                customer_id,
                order_status,
                created_at,
                updated_at
            FROM UNNEST(@Orders)
            RETURNING 
                id,
                order_id,
                order_item_id,
                customer_id,
                order_status,
                created_at,
                updated_at
            ";

            var conn = await unitOfWork.GetConnection(token);
       
            var res = await conn.QueryAsync<V1AuditLogOrderDal>(new CommandDefinition(
                sql, new { Orders = model }, cancellationToken: token));

            return res.ToArray();
        }

        public async Task<V1AuditLogOrderDal[]> GetByOrderIdAsync(long orderId, CancellationToken token)
        {
            var sql = @"
                SELECT 
                    id,
                    order_id,
                    order_item_id,
                    customer_id,
                    order_status,
                    created_at,
                    updated_at
                FROM audit_log_order 
                WHERE order_id = @OrderId 
                ORDER BY created_at DESC
            ";

            var conn = await unitOfWork.GetConnection(token);
            var res = await conn.QueryAsync<V1AuditLogOrderDal>(new CommandDefinition(
                sql, new { OrderId = orderId }, cancellationToken: token));

            return res.ToArray();
        }

        public async Task<V1AuditLogOrderDal[]> GetByCustomerIdAsync(long customerId, CancellationToken token)
        {
            var sql = @"
                SELECT 
                    id,
                    order_id,
                    order_item_id,
                    customer_id,
                    order_status,
                    created_at,
                    updated_at
                FROM audit_log_order 
                WHERE customer_id = @CustomerId 
                ORDER BY created_at DESC
            ";

            var conn = await unitOfWork.GetConnection(token);
            var res = await conn.QueryAsync<V1AuditLogOrderDal>(new CommandDefinition(
                sql, new { CustomerId = customerId }, cancellationToken: token));

            return res.ToArray();
        }

        public async Task<V1AuditLogOrderDal[]> Query(QueryAuditLogOrderDalModel model, CancellationToken token)
        {
            var sql = new StringBuilder(@"
                SELECT 
                    id,
                    order_id,
                    order_item_id,
                    customer_id,
                    order_status,
                    created_at,
                    updated_at
                FROM audit_log_order
            ");

            var param = new DynamicParameters();
            var conditions = new List<string>();

            if (model.OrderId > 0)
            {
                param.Add("OrderId", model.OrderId);
                conditions.Add("order_id = @OrderId");
            }

            if (model.OrderItemId > 0)
            {
                param.Add("OrderItemId", model.OrderItemId);
                conditions.Add("order_item_id = @OrderItemId");
            }

            if (model.CustomerId > 0)
            {
                param.Add("CustomerId", model.CustomerId);
                conditions.Add("customer_id = @CustomerId");
            }

            if (!string.IsNullOrEmpty(model.OrderStatus))
            {
                param.Add("OrderStatus", model.OrderStatus);
                conditions.Add("order_status = @OrderStatus");
            }

            if (model.CreatedFrom.HasValue)
            {
                param.Add("CreatedFrom", model.CreatedFrom.Value);
                conditions.Add("created_at >= @CreatedFrom");
            }

            if (model.CreatedTo.HasValue)
            {
                param.Add("CreatedTo", model.CreatedTo.Value);
                conditions.Add("created_at <= @CreatedTo");
            }

            if (conditions.Count > 0)
            {
                sql.Append(" WHERE " + string.Join(" AND ", conditions));
            }

            sql.Append(" ORDER BY created_at DESC");

            if (model.Limit > 0)
            {
                sql.Append(" LIMIT @Limit");
                param.Add("Limit", model.Limit);
            }

            if (model.Offset > 0)
            {
                sql.Append(" OFFSET @Offset");
                param.Add("Offset", model.Offset);
            }

            var conn = await unitOfWork.GetConnection(token);
            var res = await conn.QueryAsync<V1AuditLogOrderDal>(new CommandDefinition(
                sql.ToString(), param, cancellationToken: token));

            return res.ToArray();
        }

        // Реализация старых методов для обратной совместимости
        public async Task AddBatchAsync(IEnumerable<V1AuditLogOrderDal> auditLogs, NpgsqlTransaction transaction = null)
        {
            var connection = await unitOfWork.GetConnection(CancellationToken.None);

            var sql = @"
                INSERT INTO audit_log_order 
                (order_id, order_item_id, customer_id, order_status, created_at, updated_at)
                VALUES (@OrderId, @OrderItemId, @CustomerId, @OrderStatus, @CreatedAt, @UpdatedAt)";

            await connection.ExecuteAsync(sql, auditLogs.Select(log => new
            {
                log.OrderId,
                log.OrderItemId,
                log.CustomerId,
                log.OrderStatus,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }), transaction);
        }

        public async Task<IEnumerable<V1AuditLogOrderDal>> GetByOrderIdAsync(long orderId)
        {
            var result = await GetByOrderIdAsync(orderId, CancellationToken.None);
            return result;
        }

        public async Task<IEnumerable<V1AuditLogOrderDal>> GetByCustomerIdAsync(long customerId)
        {
            var result = await GetByCustomerIdAsync(customerId, CancellationToken.None);
            return result;
        }
    }
}