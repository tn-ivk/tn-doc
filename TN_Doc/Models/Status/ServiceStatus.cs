namespace TN_Doc.Models.Status
{
    /// <summary>
    /// Статус внешних сервисов
    /// </summary>
    public class ServiceStatus
    {
        public ConnectionStatus MessagingService { get; set; } = new();
        public ConnectionStatus? Elis { get; set; }
    }
}