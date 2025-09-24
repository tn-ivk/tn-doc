using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TN_Doc.Models.Status
{
    /// <summary>
    /// Статус OPC сервера
    /// </summary>
    public class OpcServerStatus
    {
        /// <summary>
        /// Идентификатор OPC сервера
        /// </summary>
        [Required]
        public string OpcId { get; set; } = string.Empty;

        /// <summary>
        /// Тип OPC сервера (DA, UA)
        /// </summary>
        [Required]
        public OpcServerType ServerType { get; set; }

        /// <summary>
        /// Наименование OPC сервера
        /// </summary>
        [Required]
        public string ServerName { get; set; } = string.Empty;

        /// <summary>
        /// Статус подключения к OPC серверу
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
        /// Endpoint или ProgID сервера
        /// </summary>
        public string ServerAddress { get; set; } = string.Empty;

        /// <summary>
        /// Статус сертификата (для OPC UA)
        /// </summary>
        public CertificateStatus? CertificateStatus { get; set; }

        /// <summary>
        /// Количество активных групп/подписок
        /// </summary>
        public int ActiveGroups { get; set; }

        /// <summary>
        /// Количество активных тегов
        /// </summary>
        public int ActiveTags { get; set; }

        /// <summary>
        /// Сообщение об ошибке (если есть)
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Код ошибки OPC (если есть)
        /// </summary>
        public int? OpcErrorCode { get; set; }

        /// <summary>
        /// Версия OPC сервера
        /// </summary>
        public string? ServerVersion { get; set; }

        /// <summary>
        /// Vendor информация
        /// </summary>
        public string? VendorInfo { get; set; }

        /// <summary>
        /// Время последнего успешного соединения
        /// </summary>
        public DateTime? LastSuccessfulConnection { get; set; }

        /// <summary>
        /// Количество попыток переподключения
        /// </summary>
        public int RetryCount { get; set; }
    }

    /// <summary>
    /// Тип OPC сервера
    /// </summary>
    public enum OpcServerType
    {
        /// <summary>
        /// OPC Data Access (COM/DCOM)
        /// </summary>
        OPC_DA,

        /// <summary>
        /// OPC Unified Architecture
        /// </summary>
        OPC_UA
    }

    /// <summary>
    /// Статус сертификата OPC UA
    /// </summary>
    public class CertificateStatus
    {
        /// <summary>
        /// Сертификат действителен
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Дата истечения сертификата
        /// </summary>
        public DateTime? ExpiryDate { get; set; }

        /// <summary>
        /// Субъект сертификата
        /// </summary>
        public string? Subject { get; set; }

        /// <summary>
        /// Издатель сертификата
        /// </summary>
        public string? Issuer { get; set; }

        /// <summary>
        /// Ошибки валидации сертификата
        /// </summary>
        public List<string>? ValidationErrors { get; set; }
    }
}