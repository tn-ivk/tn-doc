namespace TN_Doc.Models.Status
{
    /// <summary>
    /// Типы подключений для мониторинга
    /// </summary>
    public enum ConnectionType
    {
        /// <summary>
        /// Подключение к базе данных MySQL/MariaDB
        /// </summary>
        Database = 1,

        /// <summary>
        /// OPC Data Access подключение
        /// </summary>
        OpcDA = 2,

        /// <summary>
        /// OPC Unified Architecture подключение
        /// </summary>
        OpcUA = 3,

        /// <summary>
        /// SignalR Hub подключение
        /// </summary>
        SignalR = 4,

        /// <summary>
        /// ELIS (Единая Лабораторная Информационная Система) подключение
        /// </summary>
        Elis = 5,

        /// <summary>
        /// HTTP/HTTPS веб-сервис
        /// </summary>
        Http = 6,

        /// <summary>
        /// TCP/IP сокет подключение
        /// </summary>
        Tcp = 7,

        /// <summary>
        /// UDP подключение
        /// </summary>
        Udp = 8,

        /// <summary>
        /// File system мониторинг
        /// </summary>
        FileSystem = 9,

        /// <summary>
        /// Windows Service мониторинг
        /// </summary>
        WindowsService = 10,

        /// <summary>
        /// Linux/Unix System Service мониторинг
        /// </summary>
        SystemdService = 11,

        /// <summary>
        /// Пользовательский тип подключения
        /// </summary>
        Custom = 999
    }

    /// <summary>
    /// Расширения для работы с типами подключений
    /// </summary>
    public static class ConnectionTypeExtensions
    {
        /// <summary>
        /// Получение отображаемого наименования типа подключения
        /// </summary>
        public static string GetDisplayName(this ConnectionType connectionType)
        {
            return connectionType switch
            {
                ConnectionType.Database => "База данных",
                ConnectionType.OpcDA => "OPC DA",
                ConnectionType.OpcUA => "OPC UA",
                ConnectionType.SignalR => "SignalR",
                ConnectionType.Elis => "ELIS",
                ConnectionType.Http => "HTTP/HTTPS",
                ConnectionType.Tcp => "TCP/IP",
                ConnectionType.Udp => "UDP",
                ConnectionType.FileSystem => "Файловая система",
                ConnectionType.WindowsService => "Windows Service",
                ConnectionType.SystemdService => "Systemd Service",
                ConnectionType.Custom => "Пользовательский",
                _ => "Неизвестный"
            };
        }

        /// <summary>
        /// Получение CSS класса для типа подключения
        /// </summary>
        public static string GetCssClass(this ConnectionType connectionType)
        {
            return connectionType switch
            {
                ConnectionType.Database => "status-database",
                ConnectionType.OpcDA => "status-opc",
                ConnectionType.OpcUA => "status-opc",
                ConnectionType.SignalR => "status-signalr",
                ConnectionType.Elis => "status-elis",
                ConnectionType.Http => "status-http",
                ConnectionType.Tcp => "status-tcp",
                ConnectionType.Udp => "status-udp",
                ConnectionType.FileSystem => "status-filesystem",
                ConnectionType.WindowsService => "status-service",
                ConnectionType.SystemdService => "status-service",
                ConnectionType.Custom => "status-custom",
                _ => "status-unknown"
            };
        }

        /// <summary>
        /// Получение рекомендуемого интервала проверки в секундах
        /// </summary>
        public static int GetRecommendedCheckInterval(this ConnectionType connectionType)
        {
            return connectionType switch
            {
                ConnectionType.Database => 60, // 1 минута
                ConnectionType.OpcDA => 30, // 30 секунд
                ConnectionType.OpcUA => 30, // 30 секунд
                ConnectionType.SignalR => 10, // 10 секунд
                ConnectionType.Elis => 120, // 2 минуты
                ConnectionType.Http => 60, // 1 минута
                ConnectionType.Tcp => 30, // 30 секунд
                ConnectionType.Udp => 30, // 30 секунд
                ConnectionType.FileSystem => 300, // 5 минут
                ConnectionType.WindowsService => 60, // 1 минута
                ConnectionType.SystemdService => 60, // 1 минута
                ConnectionType.Custom => 60, // 1 минута
                _ => 60
            };
        }

        /// <summary>
        /// Получение рекомендуемого таймаута в секундах
        /// </summary>
        public static int GetRecommendedTimeout(this ConnectionType connectionType)
        {
            return connectionType switch
            {
                ConnectionType.Database => 5, // 5 секунд
                ConnectionType.OpcDA => 3, // 3 секунды
                ConnectionType.OpcUA => 5, // 5 секунд
                ConnectionType.SignalR => 3, // 3 секунды
                ConnectionType.Elis => 10, // 10 секунд (внешний API)
                ConnectionType.Http => 5, // 5 секунд
                ConnectionType.Tcp => 3, // 3 секунды
                ConnectionType.Udp => 3, // 3 секунды
                ConnectionType.FileSystem => 2, // 2 секунды
                ConnectionType.WindowsService => 5, // 5 секунд
                ConnectionType.SystemdService => 5, // 5 секунд
                ConnectionType.Custom => 5, // 5 секунд
                _ => 5
            };
        }
    }
}