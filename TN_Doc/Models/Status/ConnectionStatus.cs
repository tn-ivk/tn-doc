using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TN_Doc.Models.Status
{
    /// <summary>
    /// Базовый статус подключения
    /// </summary>
    public class ConnectionStatus
    {
        /// <summary>
        /// Статус подключения
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
        /// Сообщение об ошибке (если есть)
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Тип подключения
        /// </summary>
        public ConnectionType ConnectionType { get; set; }

        /// <summary>
        /// Количество попыток переподключения
        /// </summary>
        public int RetryCount { get; set; }

        /// <summary>
        /// Время следующей попытки проверки
        /// </summary>
        public DateTime? NextCheckTime { get; set; }

        /// <summary>
        /// Статистика подключения
        /// </summary>
        public ConnectionStatistics? Statistics { get; set; }
    }

    /// <summary>
    /// Статистика подключения
    /// </summary>
    public class ConnectionStatistics
    {
        /// <summary>
        /// Общее количество проверок
        /// </summary>
        public int TotalChecks { get; set; }

        /// <summary>
        /// Количество успешных подключений
        /// </summary>
        public int SuccessfulConnections { get; set; }

        /// <summary>
        /// Количество неудачных подключений
        /// </summary>
        public int FailedConnections { get; set; }

        /// <summary>
        /// Среднее время отклика
        /// </summary>
        public double AverageResponseTime { get; set; }

        /// <summary>
        /// Процент успешности подключений
        /// </summary>
        public double SuccessRate => TotalChecks > 0 ? (double)SuccessfulConnections / TotalChecks * 100 : 0;

        /// <summary>
        /// Время первой проверки
        /// </summary>
        public DateTime FirstCheckTime { get; set; }

        /// <summary>
        /// Время восстановления соединения (аптайм)
        /// </summary>
        public TimeSpan? Uptime { get; set; }
    }
}