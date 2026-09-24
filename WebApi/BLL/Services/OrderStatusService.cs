using System;
using System.Collections.Generic;

namespace WebApi.BLL.Services
{
    public static class OrderStatusService
    {
        public const string Created = "created";
        public const string Processing = "processing";
        public const string Completed = "completed";
        public const string Cancelled = "cancelled";

        private static readonly Dictionary<string, HashSet<string>> AllowedTransitions = 
            new Dictionary<string, HashSet<string>>
            {
                [Created] = new HashSet<string> { Processing, Cancelled },
                [Processing] = new HashSet<string> { Completed, Cancelled },
                [Completed] = new HashSet<string>(),
                [Cancelled] = new HashSet<string>()
            };
        
        public static bool IsTransitionAllowed(string currentStatus, string newStatus)
        {
            if (string.IsNullOrEmpty(currentStatus) || string.IsNullOrEmpty(newStatus))
                return false;
                
            if (!AllowedTransitions.ContainsKey(currentStatus.ToLower()))
                return false;
                
            return AllowedTransitions[currentStatus.ToLower()].Contains(newStatus.ToLower());
        }
        
        public static string[] GetAllStatuses()
        {
            return new[] { Created, Processing, Completed, Cancelled };
        }
        
        // Проверяем, является ли статус валидным
        public static bool IsValidStatus(string status)
        {
            if (string.IsNullOrEmpty(status))
                return false;
                
            return AllowedTransitions.ContainsKey(status.ToLower());
        }
        
        // Валидируем переход статуса, выбрасывает исключение при невалидном переходе
        public static void ValidateTransition(string currentStatus, string newStatus)
        {
            if (!IsValidStatus(currentStatus))
                throw new ArgumentException($"Invalid current status: {currentStatus}");
                
            if (!IsValidStatus(newStatus))
                throw new ArgumentException($"Invalid new status: {newStatus}");
                
            if (!IsTransitionAllowed(currentStatus, newStatus))
                throw new InvalidOperationException(
                    $"Transition from '{currentStatus}' to '{newStatus}' is not allowed.");
        }
    }
}