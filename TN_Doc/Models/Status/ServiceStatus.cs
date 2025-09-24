using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TN_Doc.Models.Status
{
    /// <summary>
    /// Статус внешнего сервиса
    /// </summary>
    public class ServiceStatus
    {
        /// <summary>
        /// Наименование сервиса
        /// </summary>
        [Required]
        public string ServiceName { get; set; } = string.Empty;

        /// <summary>
        /// Доступность сервиса
        /// </summary>
        public bool IsAvailable { get; set; }

        /// <summary>
        /// Время последней проверки
        /// </summary>
        public DateTime LastCheck { get; set; }

        /// <summary>
        /// URL или адрес сервиса
        /// </summary>
        public string Url { get; set; } = string.Empty;

        /// <summary>
        /// Описание статуса
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Время отклика в миллисекундах
        /// </summary>
        public long ResponseTime { get; set; }

        /// <summary>
        /// Сообщение об ошибке (если есть)
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Версия сервиса (если доступна)
        /// </summary>
        public string? Version { get; set; }

        /// <summary>
        /// Дополнительные метаданные сервиса
        /// </summary>
        public Dictionary<string, object>? Metadata { get; set; }
    }
}