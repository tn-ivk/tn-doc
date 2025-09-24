using System;
using System.ComponentModel.DataAnnotations;

namespace TN_Doc.Models.Status
{
    /// <summary>
    /// Статус подключения к устройству ИВК
    /// </summary>
    public class DeviceStatus
    {
        /// <summary>
        /// Идентификатор устройства
        /// </summary>
        [Required]
        public int DeviceId { get; set; }

        /// <summary>
        /// Наименование устройства
        /// </summary>
        [Required]
        public string DeviceName { get; set; } = string.Empty;

        /// <summary>
        /// Статус подключения к БД устройства
        /// </summary>
        public bool IsConnected { get; set; }

        /// <summary>
        /// Время последней проверки
        /// </summary>
        public DateTime LastCheck { get; set; }

        /// <summary>
        /// Время отклика в миллисекундах
        /// </summary>
        public long ResponseTime { get; set; }

        /// <summary>
        /// Дополнительная информация о подключении
        /// </summary>
        public string ConnectionInfo { get; set; } = string.Empty;

        /// <summary>
        /// Строка подключения к БД (замаскированная)
        /// </summary>
        public string ConnectionString { get; set; } = string.Empty;

        /// <summary>
        /// Сообщение об ошибке (если есть)
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Количество попыток переподключения
        /// </summary>
        public int RetryCount { get; set; }
    }
}