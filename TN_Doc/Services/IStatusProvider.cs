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

        /// <summary>
        /// Проверить статус конкретного устройства (с принудительной проверкой)
        /// </summary>
        /// <param name="deviceId">ID устройства</param>
        /// <param name="ct">Токен отмены</param>
        /// <returns>Статус устройства или null если устройство не найдено</returns>
        Task<DeviceStatus?> CheckDeviceAsync(string deviceId, CancellationToken ct = default);
    }
}