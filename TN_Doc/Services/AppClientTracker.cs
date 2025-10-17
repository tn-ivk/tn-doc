using System;
using System.Collections.Concurrent;

namespace TN_Doc.Services;

/// <summary>
/// Отслеживает активность HTTP-клиентов приложения.
/// </summary>
public sealed class AppClientTracker
{
    private readonly ConcurrentDictionary<string, DateTime> _clients = new();

    /// <summary>
    /// Регистрирует обращение клиента.
    /// </summary>
    /// <param name="clientId">Уникальный идентификатор клиента.</param>
    public void RegisterRequest(string? clientId)
    {
        if (string.IsNullOrWhiteSpace(clientId))
        {
            return;
        }

        _clients[clientId] = DateTime.UtcNow;
    }

    /// <summary>
    /// Удаляет записи неактивных клиентов.
    /// </summary>
    /// <param name="maxInactivity">Допустимое время неактивности.</param>
    public void RemoveInactiveClients(TimeSpan maxInactivity)
    {
        var threshold = DateTime.UtcNow - maxInactivity;

        foreach (var entry in _clients)
        {
            if (entry.Value < threshold)
            {
                _clients.TryRemove(entry.Key, out _);
            }
        }
    }

    /// <summary>
    /// Возвращает признак наличия активных клиентов.
    /// </summary>
    public bool HasActiveClients => !_clients.IsEmpty;

    /// <summary>
    /// Возвращает количество активных клиентов.
    /// </summary>
    public int ActiveClientCount => _clients.Count;
}

