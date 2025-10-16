using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using TN_Doc.Services;

namespace TN_Doc.Middleware;

/// <summary>
/// Middleware для регистрации активности HTTP-клиентов приложения.
/// </summary>
public sealed class AppClientTrackingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly AppClientTracker _clientTracker;
    private readonly TimeSpan _maxInactivity;

    public AppClientTrackingMiddleware(RequestDelegate next, AppClientTracker clientTracker, TimeSpan? maxInactivity = null)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _clientTracker = clientTracker ?? throw new ArgumentNullException(nameof(clientTracker));
        _maxInactivity = maxInactivity ?? TimeSpan.FromMinutes(2);
    }

    public Task InvokeAsync(HttpContext context)
    {
        RegisterClient(context);

        // Чистим устаревшие записи на лету, без отдельного фонового задания.
        _clientTracker.RemoveInactiveClients(_maxInactivity);

        return _next(context);
    }

    private void RegisterClient(HttpContext context)
    {
        var clientId = ResolveClientId(context);
        _clientTracker.RegisterRequest(clientId);
    }

    private static string ResolveClientId(HttpContext context)
    {
        // Используем комбинацию IP + UserAgent как простой идентификатор клиента.
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        context.Request.Headers.TryGetValue("User-Agent", out var userAgent);
        return $"{ip}:{userAgent}";
    }
}

