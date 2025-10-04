using System.Threading;

namespace TN_Doc.Services;

/// <summary>
/// Сервис для отслеживания активных подключений к StatusHub
/// </summary>
public class ConnectionTracker
{
    private int _connectionCount = 0;

    /// <summary>
    /// Увеличивает счетчик подключений
    /// </summary>
    public void IncrementConnections()
    {
        Interlocked.Increment(ref _connectionCount);
    }

    /// <summary>
    /// Уменьшает счетчик подключений
    /// </summary>
    public void DecrementConnections()
    {
        Interlocked.Decrement(ref _connectionCount);
    }

    /// <summary>
    /// Возвращает true, если есть активные подключения
    /// </summary>
    public bool HasActiveConnections => _connectionCount > 0;

    /// <summary>
    /// Возвращает количество активных подключений
    /// </summary>
    public int ActiveConnectionCount => _connectionCount;
}
