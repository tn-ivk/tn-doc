using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace TN_Doc.Controllers;

/// <summary>
/// Контроллер обработки данных от ЕЛИС
/// </summary>
public class ElisController : Controller
{
    private readonly ILogger<ElisController> _logger;
    
    public ElisController(ILogger<ElisController> logger)
    {
        _logger = logger;
    }
    
    /// <summary>
    /// Сообщения о ошибке в данных ЕЛИС 
    /// </summary>
    /// <param name="msg">сообщений</param>
    [HttpPost]
    [AllowAnonymous]
    public void ErrorMessage(string msg)
    {
        if(string.IsNullOrEmpty(msg))
            return;
        
        var errPatterns = new List<(string pattern, string description)>
        {
            ("сообщение, подпись, или соподписи модифицированы", "Нарушена целостность системы, и не пройдена проверка по безопасности"),
            ("Электронная подпись не соответствует", "Подпись в сообщении от ЭЛИС не соответствует ожидаемой ТСПД"),
            ("Неверный сертификат", "Система не может найти сертификат и использовать его"),
            ("2035 MQRC_NOT_AUTHORIZED", "Ошибка связи с менеджером сообщений IBMMQ, невозможность логина к очереди"),
            ("ASN1 coorupted data", "Подпись была повреждена и является не читаемой"),
            ("CompCode", "Ошибки связанные с сетевыми настройками и подключения к очереди IBMMQ"),
        };
        foreach (var item in errPatterns)
        {
            if (Regex.IsMatch(msg, item.pattern, RegexOptions.IgnoreCase))
            {
                msg += ". " + item.description;
                break;
            }
        }    
        _logger.LogError(msg);   
        //EventLog.WriteEntry($".NET Runtime", msg, EventLogEntryType.Error, 1000, 1);
    }
    
    public void WarnMessage(string msg) => _logger.LogWarning(msg);
}