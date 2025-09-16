# План реализации строки состояния

## 📋 Обзор задачи

Добавление строки состояния в главное окно приложения TN_Doc для отображения статуса подключений к устройствам и внешним сервисам в реальном времени.

## 🎯 Требования

### Базовый функционал
- **Индикаторы устройств**: Отображение статуса подключения к БД для всех сконфигурированных устройств (ИВК-1, ИВК-2, ИВК-РСУ)
- **Индикатор SignalR**: Статус подключения к hubConnection `http://localhost:5010/SignalRApp`
- **Динамическое обновление**: Цветовая индикация изменяется в реальном времени
- **Расширяемость**: Архитектура должна поддерживать добавление новых индикаторов

### Визуальные требования
- **Текстовые метки** с наименованием устройства
- **Цветовая индикация**:
  - 🟢 Зелёный - подключение активно
  - 🔴 Красный - подключение отсутствует

## 🏗️ Архитектура решения

### 1. Frontend компоненты

#### 1.1 HTML структура (Views/Home/Index.cshtml)
```html
<!-- Добавить в конец body, перед закрывающим тегом -->
<div id="status-bar" class="status-bar">
    <div id="device-indicators" class="status-section">
        <!-- Динамически генерируемые индикаторы устройств -->
    </div>
    <div id="service-indicators" class="status-section">
        <span id="signalr-status" class="status-indicator" data-service="signalr">
            SignalR: <span class="status-text">Отключено</span>
        </span>
    </div>
    <div id="system-info" class="status-section">
        <span id="app-version" class="status-info">v1.4.1</span>
        <span id="current-time" class="status-info"></span>
    </div>
</div>
```

#### 1.2 CSS стили (wwwroot/css/statusBar.css)
```css
.status-bar {
    position: fixed;
    bottom: 0;
    left: 0;
    right: 0;
    height: 32px;
    background: #f8f9fa;
    border-top: 1px solid #dee2e6;
    display: flex;
    align-items: center;
    padding: 0 15px;
    font-size: 12px;
    z-index: 1000;
}

.status-section {
    display: flex;
    align-items: center;
    gap: 15px;
    margin-right: 20px;
}

.status-indicator {
    padding: 2px 8px;
    border-radius: 12px;
    font-weight: 500;
    transition: all 0.3s ease;
}

/* Состояния подключения */
.status-connected { background: #d4edda; color: #155724; }
.status-disconnected { background: #f8d7da; color: #721c24; }
.status-checking { background: #fff3cd; color: #856404; }
.status-disabled { background: #e2e3e5; color: #6c757d; }
```

#### 1.3 JavaScript управление (wwwroot/js/statusBar.js)
```javascript
class StatusBarManager {
    constructor() {
        this.indicators = new Map();
        this.signalRConnection = null;
        this.updateInterval = 30000; // 30 секунд
        this.init();
    }

    init() {
        this.createDeviceIndicators();
        this.initializeSignalR();
        this.startPeriodicUpdates();
        this.updateTime();
    }

    createDeviceIndicators() {
        // Получение списка устройств из конфигурации
        this.loadDeviceConfiguration();
    }

    updateDeviceStatus(deviceId, isConnected) {
        // Обновление статуса устройства
    }

    initializeSignalR() {
        // Инициализация SignalR подключения
    }
}
```

### 2. Backend компоненты

#### 2.1 StatusController (Controllers/StatusController.cs)
```csharp
[ApiController]
[Route("api/[controller]")]
public class StatusController : ControllerBase
{
    private readonly IAppConfigService _configService;
    private readonly IConnectionStatusService _statusService;

    [HttpGet("devices")]
    public async Task<IActionResult> GetDeviceStatuses()
    {
        var devices = _configService.GetAppCfg().Devices;
        var statuses = new List<DeviceStatus>();

        foreach (var device in devices)
        {
            var status = await _statusService.CheckDeviceConnection(device.IdDevice);
            statuses.Add(new DeviceStatus
            {
                DeviceId = device.IdDevice,
                DeviceName = device.Name,
                IsConnected = status.IsConnected,
                LastCheck = status.LastCheck,
                ConnectionString = status.ConnectionInfo
            });
        }

        return Ok(statuses);
    }

    [HttpGet("services")]
    public async Task<IActionResult> GetServiceStatuses()
    {
        return Ok(new
        {
            SignalR = await _statusService.CheckSignalRConnection()
        });
    }
}
```

#### 2.2 IConnectionStatusService
```csharp
public interface IConnectionStatusService
{
    Task<ConnectionStatus> CheckDeviceConnection(int deviceId);
    Task<ServiceStatus> CheckSignalRConnection();
    event EventHandler<DeviceStatusChangedEventArgs> DeviceStatusChanged;
}

public class ConnectionStatusService : IConnectionStatusService
{
    private readonly IAppConfigService _configService;
    private readonly ILogger<ConnectionStatusService> _logger;
    private readonly ConcurrentDictionary<int, ConnectionStatus> _deviceStatuses;

    public async Task<ConnectionStatus> CheckDeviceConnection(int deviceId)
    {
        try
        {
            var connectionString = _configService.GetConnectionString(deviceId);
            using var connection = new MySqlConnection(connectionString);

            var stopwatch = Stopwatch.StartNew();
            await connection.OpenAsync();
            stopwatch.Stop();

            var status = new ConnectionStatus
            {
                IsConnected = connection.State == ConnectionState.Open,
                ResponseTime = stopwatch.ElapsedMilliseconds,
                LastCheck = DateTime.Now,
                ConnectionInfo = $"Подключено за {stopwatch.ElapsedMilliseconds}мс"
            };

            _deviceStatuses.AddOrUpdate(deviceId, status, (key, old) => status);
            return status;
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Ошибка подключения к устройству {DeviceId}: {Error}", deviceId, ex.Message);

            var status = new ConnectionStatus
            {
                IsConnected = false,
                LastCheck = DateTime.Now,
                ConnectionInfo = $"Ошибка: {ex.Message}"
            };

            _deviceStatuses.AddOrUpdate(deviceId, status, (key, old) => status);
            return status;
        }
    }
}
```

#### 2.3 SignalR Hub для реального времени
```csharp
[Hub]
public class StatusHub : Hub
{
    public async Task JoinStatusGroup()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "StatusUpdates");
    }

    public async Task LeaveStatusGroup()
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "StatusUpdates");
    }
}
```

### 3. Модели данных

#### 3.1 Status Models (Models/Status/)
```csharp
public class DeviceStatus
{
    public int DeviceId { get; set; }
    public string DeviceName { get; set; }
    public bool IsConnected { get; set; }
    public DateTime LastCheck { get; set; }
    public long ResponseTime { get; set; }
    public string ConnectionInfo { get; set; }
}

public class ServiceStatus
{
    public string ServiceName { get; set; }
    public bool IsAvailable { get; set; }
    public DateTime LastCheck { get; set; }
    public string Url { get; set; }
    public string Status { get; set; }
}

public class ConnectionStatus
{
    public bool IsConnected { get; set; }
    public DateTime LastCheck { get; set; }
    public long ResponseTime { get; set; }
    public string ConnectionInfo { get; set; }
}
```

## 🔧 План реализации

### Этап 1: Базовая инфраструктура (1-2 дня)
1. ✅ Создание CSS стилей для строки состояния
2. ✅ Добавление HTML разметки в Index.cshtml
3. ✅ Создание базового JavaScript класса StatusBarManager
4. ✅ Создание StatusController с базовым API

### Этап 2: Индикаторы устройств (2-3 дня)
1. ✅ Реализация IConnectionStatusService
2. ✅ Интеграция с AppConfigService для получения списка устройств
3. ✅ Реализация проверки подключения к MySQL/MariaDB
4. ✅ Динамическое создание индикаторов на основе конфигурации
5. ✅ Обновление статуса каждые 30 секунд

### Этап 3: SignalR интеграция (1-2 дня)
1. ✅ Создание StatusHub для SignalR
2. ✅ Проверка доступности сервиса на localhost:5010
3. ✅ Реализация подключения и отслеживания статуса
4. ✅ Обработка переподключений и таймаутов

### Этап 4: Real-time обновления (1-2 дня)
1. ✅ Настройка SignalR для push-уведомлений
2. ✅ Реализация фонового сервиса для мониторинга
3. ✅ Оптимизация частоты проверок
4. ✅ Обработка состояний "проверка подключения"

### Этап 5: Тестирование и оптимизация (1-2 дня)
1. ✅ Unit тесты для ConnectionStatusService
2. ✅ Integration тесты для StatusController
3. ✅ Тестирование UI отзывчивости
4. ✅ Оптимизация производительности
5. ✅ Обработка edge cases

## 🔧 Технические детали

### Конфигурация
- **Интервал проверки**: 30 секунд по умолчанию (настраивается)
- **Таймаут подключения**: 5 секунд для БД, 3 секунды для SignalR
- **Retry логика**: 3 попытки с экспоненциальным backoff
- **Кэширование**: Статусы кэшируются на 10 секунд

### Интеграция с существующим кодом
- **AppConfigService**: Получение списка устройств и строк подключения
- **NLog**: Логирование событий подключения/отключения
- **Dependency Injection**: Регистрация новых сервисов в Startup.cs

### Производительность
- **Асинхронные проверки**: Все операции подключения неблокирующие
- **Параллельная проверка**: Устройства проверяются одновременно
- **Debouncing**: Предотвращение избыточных обновлений UI
- **Connection pooling**: Переиспользование подключений к БД

## 🎨 Расширяемость

### Планируемые индикаторы
- **OPC сервер**: Статус подключения к OPC DA/UA
- **ELIS сервис**: Доступность лабораторной системы
- **Файловая система**: Доступность сетевых папок
- **Принтеры**: Статус принтеров для печати документов
- **Лицензии**: Статус лицензий FastReport и других компонентов

### Архитектура для расширений
```csharp
public interface IStatusProvider
{
    string Name { get; }
    Task<ProviderStatus> GetStatusAsync();
    TimeSpan CheckInterval { get; }
}

// Пример реализации
public class OpcStatusProvider : IStatusProvider
{
    public string Name => "OPC Server";
    public TimeSpan CheckInterval => TimeSpan.FromMinutes(1);

    public async Task<ProviderStatus> GetStatusAsync()
    {
        // Проверка OPC подключения
    }
}
```

## 📝 Заметки по реализации

### Особенности MySQL подключений
- Использовать короткие таймауты для проверки статуса
- Не держать подключения открытыми после проверки
- Обрабатывать специфичные MySQL ошибки (Connection timeout, Access denied, etc.)

### SignalR Hub Connection
- URL: `http://localhost:5010/SignalRApp`
- Обработка automatic reconnection
- Graceful degradation при недоступности сервиса
- Индикация качества соединения (задержка)

### UX/UI соображения
- Строка состояния не должна перекрывать основной контент
- Анимации переходов состояния должны быть мягкими
- Tooltip с дополнительной информацией при hover
- Возможность скрытия строки состояния пользователем

## 🔍 Метрики и мониторинг

### Логирование событий
```csharp
// Примеры log событий
_logger.LogInformation("Устройство {DeviceName} подключено за {ResponseTime}мс", deviceName, responseTime);
_logger.LogWarning("Потеряно подключение к устройству {DeviceName}: {Error}", deviceName, error);
_logger.LogError("SignalR подключение недоступно: {Url}", signalRUrl);
```

### Сбор статистики
- Время отклика каждого устройства
- Частота разрывов подключения
- Среднее время восстановления соединения
- Загрузка CPU/памяти от мониторинга статуса

---

**Приоритет**: Высокий
**Сложность**: Средняя
**Время реализации**: 7-10 дней
**Зависимости**: Текущая архитектура AppConfigService, SignalR setup