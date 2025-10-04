using System.Threading;
using System.Threading.Tasks;
using TN_Doc.Models.Status;

namespace TN_Doc.Services
{
    /// <summary>
    /// Интерфейс для получения статуса устройств и сервисов
    /// </summary>
    public interface IStatusProvider
    {
        /// <summary>
        /// Получить текущий статус всех устройств и сервисов
        /// </summary>
        /// <param name="ct">Токен отмены</param>
        /// <returns>Статус всех систем</returns>
        Task<StatusResponse> GetStatusAsync(CancellationToken ct = default);
    }
}