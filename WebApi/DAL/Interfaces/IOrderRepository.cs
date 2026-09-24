using WebApi.DAL.Models;

namespace WebApi.DAL.Interfaces
{
    public interface IOrderRepository
    {
        Task<V1OrderDal[]> BulkInsert(V1OrderDal[] model, CancellationToken token);
        Task<V1OrderDal[]> Query(QueryOrdersDalModel model, CancellationToken token);
        
        // Добавляем новые методы для работы со статусами
        Task<int> UpdateOrdersStatus(long[] orderIds, string newStatus, CancellationToken token);
        Task<Dictionary<long, string>> GetOrdersStatus(long[] orderIds, CancellationToken token);
        
        // Добавляем новый метод для получения заказов по ID
        Task<V1OrderDal[]> GetOrdersByIds(long[] orderIds, CancellationToken token);
    }
}