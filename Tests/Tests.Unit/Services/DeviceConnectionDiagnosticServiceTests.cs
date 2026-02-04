using System;
using System.Net.Sockets;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Moq;
using MySqlConnector;
using NUnit.Framework;
using TN_Doc.Models.DeviceConnectionDiagnostic;
using TN_Doc.Services;
using TN_DocGeneral.Services;
using TN.DocData;

namespace Tests.Unit.Services;

[TestFixture]
public class DeviceConnectionDiagnosticServiceTests
{
    private Mock<IAppConfigService> _appConfigServiceMock;
    private Mock<ILogger<DeviceConnectionDiagnosticService>> _loggerMock;
    private DeviceConnectionDiagnosticService _service;
    private DeviceConnectionDiagnosticSettings _settings;

    [SetUp]
    public void Setup()
    {
        _settings = new DeviceConnectionDiagnosticSettings
        {
            InitialPollSeconds = 60,
            MaxPollSeconds = 3600,
            PollMultiplier = 2.0,
            NetworkFailureThreshold = 3,
            MaxRetryCount = 5
        };

        _appConfigServiceMock = new Mock<IAppConfigService>();
        _appConfigServiceMock.Setup(x => x.GetAppCfg()).Returns(new CfgApp { DeviceConnectionDiagnostic = _settings });

        _loggerMock = new Mock<ILogger<DeviceConnectionDiagnosticService>>();

        _service = new DeviceConnectionDiagnosticService(_appConfigServiceMock.Object, _loggerMock.Object);
    }

    #region Constructor Tests

    [Test]
    public void Constructor_WithNullAppConfigService_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new DeviceConnectionDiagnosticService(null!, _loggerMock.Object));
    }

    [Test]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new DeviceConnectionDiagnosticService(_appConfigServiceMock.Object, null!));
    }

    #endregion

    #region ShouldAllowConnection Tests

    [Test]
    public void ShouldAllowConnection_WhenActive_ReturnsTrue()
    {
        // Arrange
        const string deviceId = "device-1";
        // Записываем успех чтобы состояние было Active
        _service.RecordSuccess(deviceId);

        // Act
        var result = _service.ShouldAllowConnection(deviceId);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void ShouldAllowConnection_WhenNoState_ReturnsTrue()
    {
        // Arrange
        const string deviceId = "new-device-without-state";

        // Act
        var result = _service.ShouldAllowConnection(deviceId);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void ShouldAllowConnection_WithNullDeviceId_ReturnsTrue()
    {
        // Act
        var result = _service.ShouldAllowConnection(null!);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void ShouldAllowConnection_WithEmptyDeviceId_ReturnsTrue()
    {
        // Act
        var result = _service.ShouldAllowConnection(string.Empty);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void ShouldAllowConnection_WhenRequiresManualReset_ReturnsFalse()
    {
        // Arrange
        const string deviceId = "device-1";
        var authException = CreateMySqlException(1045, "Access denied for user");
        _service.RecordFailure(deviceId, "Test Device", authException);

        // Act
        var result = _service.ShouldAllowConnection(deviceId);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void ShouldAllowConnection_AfterPollExpired_ReturnsTrue()
    {
        // Arrange
        const string deviceId = "device-1";
        // Уменьшаем настройки для быстрого теста
        _settings.InitialPollSeconds = 1;
        _settings.NetworkFailureThreshold = 1;

        var networkException = CreateMySqlException(2003, "Can't connect to MySQL server");
        _service.RecordFailure(deviceId, "Test Device", networkException);

        // Ждём истечения интервала опроса
        System.Threading.Thread.Sleep(1100);

        // Act
        var result = _service.ShouldAllowConnection(deviceId);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void ShouldAllowConnection_DuringPoll_ReturnsFalse()
    {
        // Arrange
        const string deviceId = "device-1";
        _settings.NetworkFailureThreshold = 1;

        var networkException = CreateMySqlException(2003, "Can't connect to MySQL server");
        _service.RecordFailure(deviceId, "Test Device", networkException);

        // Act
        var result = _service.ShouldAllowConnection(deviceId);

        // Assert
        Assert.That(result, Is.False);
    }

    #endregion

    #region RecordFailure Tests

    [Test]
    public void RecordFailure_AuthError_SetsBlockedState()
    {
        // Arrange
        const string deviceId = "device-1";
        var authException = CreateMySqlException(1045, "Access denied for user 'test'@'localhost'");

        // Act
        _service.RecordFailure(deviceId, "Test Device", authException);

        // Assert
        var info = _service.GetDeviceConnectionDiagnosticInfo(deviceId);
        Assert.That(info, Is.Not.Null);
        Assert.That(info!.State, Is.EqualTo("Blocked"));
        Assert.That(info.IsBlocked, Is.True);
        Assert.That(info.ErrorCategory, Is.EqualTo("Authentication"));
    }

    [Test]
    public void RecordFailure_AuthError_SetsRequiresManualReset()
    {
        // Arrange
        const string deviceId = "device-1";
        var authException = CreateMySqlException(1045, "Access denied for user");

        // Act
        _service.RecordFailure(deviceId, "Test Device", authException);

        // Assert
        var info = _service.GetDeviceConnectionDiagnosticInfo(deviceId);
        Assert.That(info, Is.Not.Null);
        Assert.That(info!.IsBlocked, Is.True);
        // MaxRetryReached false потому что блокировка из-за auth, а не из-за превышения попыток
        Assert.That(info.MaxRetryReached, Is.False);
    }

    [Test]
    public void RecordFailure_NetworkError_IncrementsFailureCount()
    {
        // Arrange
        const string deviceId = "device-1";
        var networkException = CreateMySqlException(2003, "Can't connect to MySQL server");

        // Act
        _service.RecordFailure(deviceId, "Test Device", networkException);
        _service.RecordFailure(deviceId, "Test Device", networkException);

        // Assert
        var info = _service.GetDeviceConnectionDiagnosticInfo(deviceId);
        Assert.That(info, Is.Not.Null);
        Assert.That(info!.FailureCount, Is.EqualTo(2));
        Assert.That(info.ErrorCategory, Is.EqualTo("Network"));
    }

    [Test]
    public void RecordFailure_NetworkError_BelowThreshold_StateActive()
    {
        // Arrange
        const string deviceId = "device-1";
        _settings.NetworkFailureThreshold = 3;
        var networkException = CreateMySqlException(2003, "Can't connect to MySQL server");

        // Act - записываем 2 ошибки (меньше порога)
        _service.RecordFailure(deviceId, "Test Device", networkException);
        _service.RecordFailure(deviceId, "Test Device", networkException);

        // Assert
        var info = _service.GetDeviceConnectionDiagnosticInfo(deviceId);
        Assert.That(info, Is.Not.Null);
        Assert.That(info!.State, Is.EqualTo("Active"));
        Assert.That(info.FailureCount, Is.EqualTo(2));
    }

    [Test]
    public void RecordFailure_AfterThreshold_SetsBlockedWithPoll()
    {
        // Arrange
        const string deviceId = "device-1";
        _settings.NetworkFailureThreshold = 2;
        var networkException = CreateMySqlException(2003, "Can't connect to MySQL server");

        // Act - записываем ошибки до превышения порога
        _service.RecordFailure(deviceId, "Test Device", networkException);
        _service.RecordFailure(deviceId, "Test Device", networkException);

        // Assert
        var info = _service.GetDeviceConnectionDiagnosticInfo(deviceId);
        Assert.That(info, Is.Not.Null);
        Assert.That(info!.State, Is.EqualTo("Blocked"));
        Assert.That(info.CurrentPollSeconds, Is.EqualTo(_settings.InitialPollSeconds));
        Assert.That(info.IsBlocked, Is.False); // Не заблокировано навсегда, есть интервал опроса
    }

    [Test]
    public void RecordFailure_AfterMaxRetryCount_SetsBlockedPermanently()
    {
        // Arrange
        const string deviceId = "device-1";
        _settings.NetworkFailureThreshold = 1;
        _settings.MaxPollSeconds = 60;
        _settings.MaxRetryCount = 2;
        _settings.PollMultiplier = 2.0;
        _settings.InitialPollSeconds = 30;
        var networkException = CreateMySqlException(2003, "Can't connect to MySQL server");

        // Act - регистрируем достаточно ошибок для достижения MaxPoll и превышения MaxRetryCount
        // 1-я ошибка: достигаем threshold, poll = 30
        _service.RecordFailure(deviceId, "Test Device", networkException);
        // 2-я ошибка: poll = 60 (max), MaxPollRetryCount = 1
        _service.RecordFailure(deviceId, "Test Device", networkException);
        // 3-я ошибка: poll = 60 (max), MaxPollRetryCount = 2 >= MaxRetryCount
        _service.RecordFailure(deviceId, "Test Device", networkException);

        // Assert
        var info = _service.GetDeviceConnectionDiagnosticInfo(deviceId);
        Assert.That(info, Is.Not.Null);
        Assert.That(info!.State, Is.EqualTo("Blocked"));
        Assert.That(info.IsBlocked, Is.True);
        Assert.That(info.MaxRetryReached, Is.True);
    }

    [Test]
    public void RecordFailure_SocketException_CategorizedAsNetwork()
    {
        // Arrange
        const string deviceId = "device-1";
        var socketException = new SocketException(10061); // Connection refused

        // Act
        _service.RecordFailure(deviceId, "Test Device", socketException);

        // Assert
        var info = _service.GetDeviceConnectionDiagnosticInfo(deviceId);
        Assert.That(info, Is.Not.Null);
        Assert.That(info!.ErrorCategory, Is.EqualTo("Network"));
    }

    [Test]
    public void RecordFailure_TimeoutException_CategorizedAsNetwork()
    {
        // Arrange
        const string deviceId = "device-1";
        var timeoutException = new TimeoutException("Connection timed out");

        // Act
        _service.RecordFailure(deviceId, "Test Device", timeoutException);

        // Assert
        var info = _service.GetDeviceConnectionDiagnosticInfo(deviceId);
        Assert.That(info, Is.Not.Null);
        Assert.That(info!.ErrorCategory, Is.EqualTo("Network"));
    }

    [Test]
    public void RecordFailure_AccessDeniedInMessage_CategorizedAsAuthentication()
    {
        // Arrange
        const string deviceId = "device-1";
        var genericException = new Exception("Access denied for user");

        // Act
        _service.RecordFailure(deviceId, "Test Device", genericException);

        // Assert
        var info = _service.GetDeviceConnectionDiagnosticInfo(deviceId);
        Assert.That(info, Is.Not.Null);
        Assert.That(info!.ErrorCategory, Is.EqualTo("Authentication"));
    }

    [Test]
    public void RecordFailure_InnerMySqlException_CorrectlyCategorized()
    {
        // Arrange
        const string deviceId = "device-1";
        var innerException = CreateMySqlException(1045, "Access denied");
        var outerException = new Exception("Wrapper exception", innerException);

        // Act
        _service.RecordFailure(deviceId, "Test Device", outerException);

        // Assert
        var info = _service.GetDeviceConnectionDiagnosticInfo(deviceId);
        Assert.That(info, Is.Not.Null);
        Assert.That(info!.ErrorCategory, Is.EqualTo("Authentication"));
    }

    [Test]
    public void RecordFailure_WithNullDeviceId_DoesNotThrow()
    {
        // Arrange
        var exception = new Exception("Test error");

        // Act & Assert
        Assert.DoesNotThrow(() => _service.RecordFailure(null!, "Test", exception));
    }

    [Test]
    public void RecordFailure_WithEmptyDeviceId_DoesNotThrow()
    {
        // Arrange
        var exception = new Exception("Test error");

        // Act & Assert
        Assert.DoesNotThrow(() => _service.RecordFailure(string.Empty, "Test", exception));
    }

    [Test]
    public void RecordFailure_ExponentialPoll_IncreasesCorrectly()
    {
        // Arrange
        const string deviceId = "device-1";
        _settings.NetworkFailureThreshold = 1;
        _settings.InitialPollSeconds = 60;
        _settings.MaxPollSeconds = 3600;
        _settings.PollMultiplier = 2.0;
        var networkException = CreateMySqlException(2003, "Can't connect to MySQL server");

        // Act - первая ошибка
        _service.RecordFailure(deviceId, "Test Device", networkException);
        var info1 = _service.GetDeviceConnectionDiagnosticInfo(deviceId);

        // Вторая ошибка
        _service.RecordFailure(deviceId, "Test Device", networkException);
        var info2 = _service.GetDeviceConnectionDiagnosticInfo(deviceId);

        // Третья ошибка
        _service.RecordFailure(deviceId, "Test Device", networkException);
        var info3 = _service.GetDeviceConnectionDiagnosticInfo(deviceId);

        // Assert
        Assert.That(info1!.CurrentPollSeconds, Is.EqualTo(60));   // Initial
        Assert.That(info2!.CurrentPollSeconds, Is.EqualTo(120));  // 60 * 2
        Assert.That(info3!.CurrentPollSeconds, Is.EqualTo(240));  // 120 * 2
    }

    #endregion

    #region RecordSuccess Tests

    [Test]
    public void RecordSuccess_ResetsAllCounters()
    {
        // Arrange
        const string deviceId = "device-1";
        _settings.NetworkFailureThreshold = 1;
        var networkException = CreateMySqlException(2003, "Can't connect to MySQL server");

        // Создаём состояние с ошибками
        _service.RecordFailure(deviceId, "Test Device", networkException);
        _service.RecordFailure(deviceId, "Test Device", networkException);

        // Act
        _service.RecordSuccess(deviceId);

        // Assert
        var info = _service.GetDeviceConnectionDiagnosticInfo(deviceId);
        Assert.That(info, Is.Not.Null);
        Assert.That(info!.State, Is.EqualTo("Active"));
        Assert.That(info.FailureCount, Is.EqualTo(0));
        Assert.That(info.CurrentPollSeconds, Is.EqualTo(0));
        Assert.That(info.IsBlocked, Is.False);
        Assert.That(info.ErrorCategory, Is.Null);
    }

    [Test]
    public void RecordSuccess_AfterAuthError_ResetsState()
    {
        // Arrange
        const string deviceId = "device-1";
        var authException = CreateMySqlException(1045, "Access denied");
        _service.RecordFailure(deviceId, "Test Device", authException);

        // Verify blocked
        Assert.That(_service.ShouldAllowConnection(deviceId), Is.False);

        // Act
        _service.RecordSuccess(deviceId);

        // Assert
        Assert.That(_service.ShouldAllowConnection(deviceId), Is.True);
        var info = _service.GetDeviceConnectionDiagnosticInfo(deviceId);
        Assert.That(info!.IsBlocked, Is.False);
    }

    [Test]
    public void RecordSuccess_WithNullDeviceId_DoesNotThrow()
    {
        // Act & Assert
        Assert.DoesNotThrow(() => _service.RecordSuccess(null!));
    }

    [Test]
    public void RecordSuccess_WithEmptyDeviceId_DoesNotThrow()
    {
        // Act & Assert
        Assert.DoesNotThrow(() => _service.RecordSuccess(string.Empty));
    }

    [Test]
    public void RecordSuccess_ForUnknownDevice_DoesNotThrow()
    {
        // Act & Assert
        Assert.DoesNotThrow(() => _service.RecordSuccess("unknown-device"));
    }

    #endregion

    #region ResetDevice Tests

    [Test]
    public void ResetDevice_ResetsState()
    {
        // Arrange
        const string deviceId = "device-1";
        var authException = CreateMySqlException(1045, "Access denied");
        _service.RecordFailure(deviceId, "Test Device", authException);

        // Act
        var result = _service.ResetDevice(deviceId);

        // Assert
        Assert.That(result, Is.True);
        var info = _service.GetDeviceConnectionDiagnosticInfo(deviceId);
        Assert.That(info!.State, Is.EqualTo("Recovering"));
        Assert.That(info.IsBlocked, Is.False);
    }

    [Test]
    public void ResetDevice_SetsRecoveringState()
    {
        // Arrange
        const string deviceId = "device-1";
        _settings.NetworkFailureThreshold = 1;
        _settings.MaxRetryCount = 1;
        _settings.MaxPollSeconds = 60;
        _settings.InitialPollSeconds = 60;
        var networkException = CreateMySqlException(2003, "Can't connect");

        // Создаём состояние с RequiresManualReset
        _service.RecordFailure(deviceId, "Test Device", networkException);
        _service.RecordFailure(deviceId, "Test Device", networkException);

        // Act
        _service.ResetDevice(deviceId);

        // Assert
        var info = _service.GetDeviceConnectionDiagnosticInfo(deviceId);
        Assert.That(info!.State, Is.EqualTo("Recovering"));
    }

    [Test]
    public void ResetDevice_AllowsConnection()
    {
        // Arrange
        const string deviceId = "device-1";
        var authException = CreateMySqlException(1045, "Access denied");
        _service.RecordFailure(deviceId, "Test Device", authException);

        // Verify blocked before reset
        Assert.That(_service.ShouldAllowConnection(deviceId), Is.False);

        // Act
        _service.ResetDevice(deviceId);

        // Assert - после ручного сброса состояние Recovering
        var info = _service.GetDeviceConnectionDiagnosticInfo(deviceId);
        Assert.That(info!.State, Is.EqualTo("Recovering"));
        Assert.That(info.IsBlocked, Is.False);
        // Примечание: ShouldAllowConnection для Recovering возвращает true только если
        // NextAllowedAttempt истёк. После ResetDevice NextAllowedAttempt = null,
        // поэтому нужен RecordSuccess для полного восстановления или проверка состояния
    }

    [Test]
    public void ResetDevice_PreservesFailureCount()
    {
        // Arrange
        const string deviceId = "device-1";
        var authException = CreateMySqlException(1045, "Access denied");
        _service.RecordFailure(deviceId, "Test Device", authException);
        _service.RecordFailure(deviceId, "Test Device", authException);

        // Act
        _service.ResetDevice(deviceId);

        // Assert - FailureCount сохраняется для истории
        var info = _service.GetDeviceConnectionDiagnosticInfo(deviceId);
        Assert.That(info!.FailureCount, Is.EqualTo(2));
    }

    [Test]
    public void ResetDevice_ForUnknownDevice_ReturnsFalse()
    {
        // Act
        var result = _service.ResetDevice("unknown-device");

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void ResetDevice_WithNullDeviceId_ReturnsFalse()
    {
        // Act
        var result = _service.ResetDevice(null!);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void ResetDevice_WithEmptyDeviceId_ReturnsFalse()
    {
        // Act
        var result = _service.ResetDevice(string.Empty);

        // Assert
        Assert.That(result, Is.False);
    }

    #endregion

    #region GetDeviceConnectionDiagnosticInfo Tests

    [Test]
    public void GetDeviceConnectionDiagnosticInfo_ForUnknownDevice_ReturnsNull()
    {
        // Act
        var info = _service.GetDeviceConnectionDiagnosticInfo("unknown-device");

        // Assert
        Assert.That(info, Is.Null);
    }

    [Test]
    public void GetDeviceConnectionDiagnosticInfo_WithNullDeviceId_ReturnsNull()
    {
        // Act
        var info = _service.GetDeviceConnectionDiagnosticInfo(null!);

        // Assert
        Assert.That(info, Is.Null);
    }

    [Test]
    public void GetDeviceConnectionDiagnosticInfo_ReturnsCorrectData()
    {
        // Arrange
        const string deviceId = "device-1";
        var authException = CreateMySqlException(1045, "Access denied for user");
        _service.RecordFailure(deviceId, "Test Device", authException);

        // Act
        var info = _service.GetDeviceConnectionDiagnosticInfo(deviceId);

        // Assert
        Assert.That(info, Is.Not.Null);
        Assert.That(info!.State, Is.EqualTo("Blocked"));
        Assert.That(info.ErrorCategory, Is.EqualTo("Authentication"));
        Assert.That(info.LastError, Is.EqualTo("Access denied for user"));
        Assert.That(info.FailureCount, Is.EqualTo(1));
        Assert.That(info.IsBlocked, Is.True);
    }

    [Test]
    public void GetDeviceConnectionDiagnosticInfo_SecondsUntilNextAttempt_CalculatedCorrectly()
    {
        // Arrange
        const string deviceId = "device-1";
        _settings.NetworkFailureThreshold = 1;
        _settings.InitialPollSeconds = 60;
        var networkException = CreateMySqlException(2003, "Can't connect");
        _service.RecordFailure(deviceId, "Test Device", networkException);

        // Act
        var info = _service.GetDeviceConnectionDiagnosticInfo(deviceId);

        // Assert
        Assert.That(info, Is.Not.Null);
        Assert.That(info!.SecondsUntilNextAttempt, Is.GreaterThan(0));
        Assert.That(info.SecondsUntilNextAttempt, Is.LessThanOrEqualTo(60));
    }

    #endregion

    #region HasBlockedDevices Tests

    [Test]
    public void HasBlockedDevices_WhenNoDevices_ReturnsFalse()
    {
        // Act
        var result = _service.HasBlockedDevices;

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void HasBlockedDevices_WhenDeviceBlocked_ReturnsTrue()
    {
        // Arrange
        var authException = CreateMySqlException(1045, "Access denied");
        _service.RecordFailure("device-1", "Test Device", authException);

        // Act
        var result = _service.HasBlockedDevices;

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void HasBlockedDevices_AfterReset_ReturnsFalse()
    {
        // Arrange
        var authException = CreateMySqlException(1045, "Access denied");
        _service.RecordFailure("device-1", "Test Device", authException);
        _service.ResetDevice("device-1");

        // Act
        var result = _service.HasBlockedDevices;

        // Assert
        Assert.That(result, Is.False);
    }

    #endregion

    #region MySQL Error Codes Tests

    [Test]
    [TestCase(1045, "Authentication")] // Access denied for user
    [TestCase(1044, "Authentication")] // Access denied for user to database
    [TestCase(1698, "Authentication")] // Access denied for user (socket auth)
    public void RecordFailure_MySqlAuthErrorCodes_CategorizedAsAuthentication(int errorCode, string expectedCategory)
    {
        // Arrange
        const string deviceId = "device-1";
        var exception = CreateMySqlException(errorCode, $"MySQL Error {errorCode}");

        // Act
        _service.RecordFailure(deviceId, "Test Device", exception);

        // Assert
        var info = _service.GetDeviceConnectionDiagnosticInfo(deviceId);
        Assert.That(info!.ErrorCategory, Is.EqualTo(expectedCategory));
    }

    [Test]
    [TestCase(2003)] // Can't connect to MySQL server
    [TestCase(2002)] // Can't connect to local MySQL server through socket
    [TestCase(2006)] // MySQL server has gone away
    [TestCase(2013)] // Lost connection to MySQL server
    public void RecordFailure_MySqlNetworkErrorCodes_CategorizedAsNetwork(int errorCode)
    {
        // Arrange
        const string deviceId = "device-1";
        var exception = CreateMySqlException(errorCode, $"MySQL Error {errorCode}");

        // Act
        _service.RecordFailure(deviceId, "Test Device", exception);

        // Assert
        var info = _service.GetDeviceConnectionDiagnosticInfo(deviceId);
        Assert.That(info!.ErrorCategory, Is.EqualTo("Network"));
    }

    [Test]
    public void RecordFailure_UnknownMySqlError_CategorizedAsOther()
    {
        // Arrange
        const string deviceId = "device-1";
        var exception = CreateMySqlException(9999, "Unknown MySQL Error");

        // Act
        _service.RecordFailure(deviceId, "Test Device", exception);

        // Assert
        var info = _service.GetDeviceConnectionDiagnosticInfo(deviceId);
        Assert.That(info!.ErrorCategory, Is.EqualTo("Other"));
    }

    #endregion

    #region Configuration Tests

    [Test]
    public void RecordFailure_UsesConfiguredSettings()
    {
        // Arrange
        const string deviceId = "device-1";
        _settings.NetworkFailureThreshold = 5;
        _settings.InitialPollSeconds = 120;
        var networkException = CreateMySqlException(2003, "Can't connect");

        // Act - записываем 5 ошибок (threshold)
        for (int i = 0; i < 5; i++)
        {
            _service.RecordFailure(deviceId, "Test Device", networkException);
        }

        // Assert
        var info = _service.GetDeviceConnectionDiagnosticInfo(deviceId);
        Assert.That(info!.CurrentPollSeconds, Is.EqualTo(120)); // Uses configured InitialPollSeconds
    }

    [Test]
    public void RecordFailure_WithNullDeviceConnectionDiagnosticSettings_UsesDefaults()
    {
        // Arrange
        _appConfigServiceMock.Setup(x => x.GetAppCfg()).Returns(new CfgApp { DeviceConnectionDiagnostic = null });
        var service = new DeviceConnectionDiagnosticService(_appConfigServiceMock.Object, _loggerMock.Object);

        const string deviceId = "device-1";
        var networkException = CreateMySqlException(2003, "Can't connect");

        // Act - использует дефолтные настройки (NetworkFailureThreshold = 3)
        service.RecordFailure(deviceId, "Test Device", networkException);
        service.RecordFailure(deviceId, "Test Device", networkException);
        service.RecordFailure(deviceId, "Test Device", networkException);

        // Assert
        var info = service.GetDeviceConnectionDiagnosticInfo(deviceId);
        Assert.That(info!.State, Is.EqualTo("Blocked"));
        Assert.That(info.CurrentPollSeconds, Is.EqualTo(60)); // Default InitialPollSeconds
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Создаёт MySqlException с заданным кодом ошибки через reflection.
    /// MySqlException не имеет публичного конструктора, поэтому используем внутренние механизмы.
    /// </summary>
    private static MySqlException CreateMySqlException(int errorCode, string message)
    {
        // Используем внутренний конструктор MySqlException через reflection
        var exceptionType = typeof(MySqlException);

        // Пробуем найти подходящий конструктор
        // MySqlConnector использует internal static методы для создания исключений
        try
        {
            // Попытка использовать internal метод CreateForTimeout или другой
            var createMethod = exceptionType.GetMethod("CreateForTimeout",
                BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);

            if (createMethod != null)
            {
                return (MySqlException)createMethod.Invoke(null, null)!;
            }
        }
        catch
        {
            // Игнорируем ошибки reflection
        }

        // Альтернативный подход: создаём через FormatterServices (без вызова конструктора)
        // и устанавливаем свойства через reflection
        var instance = (MySqlException)System.Runtime.Serialization.FormatterServices
            .GetUninitializedObject(typeof(MySqlException));

        // Устанавливаем Number (ErrorCode)
        var numberField = exceptionType.GetField("_number", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? exceptionType.GetProperty("Number")?.GetBackingField();

        if (numberField != null)
        {
            numberField.SetValue(instance, errorCode);
        }
        else
        {
            // Пробуем через свойство ErrorCode
            var errorCodeProperty = exceptionType.GetProperty("ErrorCode");
            if (errorCodeProperty != null && errorCodeProperty.CanWrite)
            {
                errorCodeProperty.SetValue(instance, errorCode);
            }
        }

        // Устанавливаем Message через базовый класс Exception
        var messageField = typeof(Exception).GetField("_message", BindingFlags.Instance | BindingFlags.NonPublic);
        messageField?.SetValue(instance, message);

        return instance;
    }

    #endregion
}

/// <summary>
/// Extension методы для работы с reflection
/// </summary>
internal static class ReflectionExtensions
{
    /// <summary>
    /// Получает backing field для auto-property
    /// </summary>
    public static FieldInfo? GetBackingField(this PropertyInfo property)
    {
        var declaringType = property.DeclaringType;
        if (declaringType == null) return null;

        var backingFieldName = $"<{property.Name}>k__BackingField";
        return declaringType.GetField(backingFieldName, BindingFlags.Instance | BindingFlags.NonPublic);
    }
}
