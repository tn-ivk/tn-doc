/**
 * Модуль логирования для клиентской части приложения
 * Предоставляет функции для отправки логов на сервер
 */

function logToServer(level, message) {
    $.ajax({
        url: '/api/ClientLog/logging',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify({ level: level, message: message }),
        error: function() { 
            // Опционально: обработка ошибок отправки лога
            console.warn('Не удалось отправить лог на сервер:', message);
        }
    });
}

/**
 * Отправляет информационное сообщение
 * @param {string} message - сообщение для логирования
 */
function logInfo(message) { 
    logToServer('Info', message); 
}

/**
 * Отправляет предупреждение
 * @param {string} message - сообщение для логирования
 */
function logWarn(message) { 
    logToServer('Warn', message); 
}

/**
 * Отправляет сообщение об ошибке
 * @param {string} message - сообщение для логирования
 */
function logError(message) { 
    logToServer('Error', message); 
}

/**
 * Отправляет отладочное сообщение
 * @param {string} message - сообщение для логирования
 */
function logDebug(message) { 
    logToServer('Debug', message); 
}

/**
 * Отправляет трассировочное сообщение
 * @param {string} message - сообщение для логирования
 */
function logTrace(message) { 
    logToServer('Trace', message); 
}
