using Dapper;
using System.Text;
using WebApi.DAL.Interfaces;
using WebApi.DAL.Models;

namespace WebApi.DAL.Repositories
{
    public class OrderRepository(UnitOfWork unitOfWork) : IOrderRepository
    {
        public async Task<V1OrderDal[]> BulkInsert(V1OrderDal[] model, CancellationToken token)
        {
            // Обновляем SQL запрос с учетом нового поля status
            var sql = @"
            insert into orders 
            (
                customer_id,
                delivery_address,
                total_price_cents,
                total_price_currency,
                status,
                created_at,
                updated_at
             )
            select 
                customer_id,
                delivery_address,
                total_price_cents,
                total_price_currency,
                status,
                created_at,
                updated_at
            from unnest(@Orders)
            returning 
                id,
                customer_id,
                delivery_address,
                total_price_cents,
                total_price_currency,
                status,
                created_at,
                updated_at;
        ";

            var conn = await unitOfWork.GetConnection(token);
            var res = await conn.QueryAsync<V1OrderDal>(new CommandDefinition(
                sql, new { Orders = model }, cancellationToken: token));

            return res.ToArray();
        }

        public async Task<V1OrderDal[]> Query(QueryOrdersDalModel model, CancellationToken token)
        {
            var sql = new StringBuilder(@"
            select 
                id,
                customer_id,
                delivery_address,
                total_price_cents,
                total_price_currency,
                status,
                created_at,
                updated_at
            from orders
        ");

            var param = new DynamicParameters();
            var conditions = new List<string>();

            if (model.Ids?.Length > 0)
            {
                param.Add("Ids", model.Ids);
                conditions.Add("id = ANY(@Ids)");
            }

            if (model.CustomerIds?.Length > 0)
            {
                param.Add("CustomerIds", model.CustomerIds);
                conditions.Add("customer_id = ANY(@CustomerIds)");
            }

            if (conditions.Count > 0)
            {
                sql.Append(" where " + string.Join(" and ", conditions));
            }

            if (model.Limit > 0)
            {
                sql.Append(" limit @Limit");
                param.Add("Limit", model.Limit);
            }

            if (model.Offset > 0)
            {
                sql.Append(" offset @Offset");
                param.Add("Offset", model.Offset);
            }

            var conn = await unitOfWork.GetConnection(token);
            var res = await conn.QueryAsync<V1OrderDal>(new CommandDefinition(
                sql.ToString(), param, cancellationToken: token));

            return res.ToArray();
        }
        
        // Реализуем новый метод для обновления статусов заказов
        public async Task<int> UpdateOrdersStatus(long[] orderIds, string newStatus, CancellationToken token)
        {
            if (orderIds == null || orderIds.Length == 0)
                return 0;
                
            var sql = @"
                UPDATE orders 
                SET status = @NewStatus, 
                    updated_at = @UpdatedAt
                WHERE id = ANY(@OrderIds)";
                
            var parameters = new
            {
                OrderIds = orderIds,
                NewStatus = newStatus,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            
            var conn = await unitOfWork.GetConnection(token);
            return await conn.ExecuteAsync(new CommandDefinition(
                sql, parameters, cancellationToken: token));
        }
        
        // Реализуем метод для получения текущих статусов заказов
        public async Task<Dictionary<long, string>> GetOrdersStatus(long[] orderIds, CancellationToken token)
        {
            if (orderIds == null || orderIds.Length == 0)
                return new Dictionary<long, string>();
                
            var sql = @"
                SELECT id, status 
                FROM orders 
                WHERE id = ANY(@OrderIds)";
                
            var parameters = new { OrderIds = orderIds };
            
            var conn = await unitOfWork.GetConnection(token);
            var results = await conn.QueryAsync<(long Id, string Status)>(
                new CommandDefinition(sql, parameters, cancellationToken: token));
                
            return results.ToDictionary(x => x.Id, x => x.Status);
        }
        
        // Добавляем новый метод для получения заказов по ID (нужен для Kafka)
        public async Task<V1OrderDal[]> GetOrdersByIds(long[] orderIds, CancellationToken token)
        {
            if (orderIds == null || orderIds.Length == 0)
                return Array.Empty<V1OrderDal>();
                
            var sql = @"
                SELECT 
                    id,
                    customer_id,
                    delivery_address,
                    total_price_cents,
                    total_price_currency,
                    status,
                    created_at,
                    updated_at
                FROM orders 
                WHERE id = ANY(@OrderIds)";
                
            var parameters = new { OrderIds = orderIds };
            
            var conn = await unitOfWork.GetConnection(token);
            var results = await conn.QueryAsync<V1OrderDal>(
                new CommandDefinition(sql, parameters, cancellationToken: token));
                
            return results.ToArray();
        }
    }
}