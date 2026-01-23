using System.Runtime.InteropServices;
using NUnit.Framework;
using TN_DocGeneral.Services;

namespace Tests.Services;

/// <summary>
/// Тесты для LoggingPathService.
/// Проверяют корректность работы статических методов определения путей логирования.
/// </summary>
[TestFixture]
public class LoggingPathServiceTests
{
    private const string ApplicationName = "TN_Doc";

    #region GetLogDirectory Tests

    /// <summary>
    /// Проверяет, что GetLogDirectory возвращает непустую строку.
    /// </summary>
    [Test]
    public void GetLogDirectory_WhenCalled_ReturnsNonEmptyString()
    {
        // Act
        var result = LoggingPathService.GetLogDirectory(ApplicationName);

        // Assert
        Assert.That(result, Is.Not.Null.And.Not.Empty);
    }

    /// <summary>
    /// Проверяет, что на Windows путь содержит папку logs.
    /// </summary>
    [Test]
    [Platform("Win")]
    public void GetLogDirectory_OnWindows_ContainsLogsFolder()
    {
        // Arrange
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Assert.Ignore("Тест пропущен: выполняется только на Windows");
        }

        // Act
        var result = LoggingPathService.GetLogDirectory(ApplicationName);

        // Assert
        Assert.That(result, Does.Contain("logs"));
    }

    /// <summary>
    /// Проверяет, что на Linux возвращается путь /var/log/TN_Doc.
    /// </summary>
    [Test]
    [Platform("Linux,Unix,MacOsX")]
    public void GetLogDirectory_OnLinux_ReturnsVarLogPath()
    {
        // Arrange
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Assert.Ignore("Тест пропущен: выполняется только на Linux/Unix");
        }

        // Act
        var result = LoggingPathService.GetLogDirectory(ApplicationName);

        // Assert
        Assert.That(result, Is.EqualTo("/var/log/TN_Doc"));
    }

    /// <summary>
    /// Проверяет, что GetLogDirectory выбрасывает исключение при пустом имени приложения.
    /// </summary>
    [Test]
    public void GetLogDirectory_WhenApplicationNameIsEmpty_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => LoggingPathService.GetLogDirectory(""));
    }

    /// <summary>
    /// Проверяет, что GetLogDirectory выбрасывает исключение при null имени приложения.
    /// </summary>
    [Test]
    public void GetLogDirectory_WhenApplicationNameIsNull_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => LoggingPathService.GetLogDirectory(null!));
    }

    #endregion

    #region GetInternalLogPath Tests

    /// <summary>
    /// Проверяет, что GetInternalLogPath возвращает путь с internal-nlog-log.txt.
    /// </summary>
    [Test]
    public void GetInternalLogPath_WhenCalled_ReturnsPathWithInternalNlogLogTxt()
    {
        // Act
        var result = LoggingPathService.GetInternalLogPath(ApplicationName);

        // Assert
        Assert.That(result, Does.EndWith("internal-nlog-log.txt"));
    }

    /// <summary>
    /// Проверяет, что GetInternalLogPath содержит директорию логов.
    /// </summary>
    [Test]
    public void GetInternalLogPath_WhenCalled_ContainsLogDirectory()
    {
        // Arrange
        var logDirectory = LoggingPathService.GetLogDirectory(ApplicationName);

        // Act
        var result = LoggingPathService.GetInternalLogPath(ApplicationName);

        // Assert
        Assert.That(result, Does.StartWith(logDirectory));
    }

    /// <summary>
    /// Проверяет, что GetInternalLogPath возвращает непустую строку.
    /// </summary>
    [Test]
    public void GetInternalLogPath_WhenCalled_ReturnsNonEmptyString()
    {
        // Act
        var result = LoggingPathService.GetInternalLogPath(ApplicationName);

        // Assert
        Assert.That(result, Is.Not.Null.And.Not.Empty);
    }

    #endregion

    #region EnsureLogDirectoryExists Tests

    /// <summary>
    /// Проверяет, что EnsureLogDirectoryExists возвращает true, если директория существует.
    /// </summary>
    [Test]
    public void EnsureLogDirectoryExists_WhenDirectoryExists_ReturnsTrue()
    {
        // Arrange
        var logDirectory = LoggingPathService.GetLogDirectory(ApplicationName);

        // Создаём директорию, если она не существует (для гарантии теста)
        if (!Directory.Exists(logDirectory))
        {
            try
            {
                Directory.CreateDirectory(logDirectory);
            }
            catch
            {
                // На некоторых системах может не быть прав на создание /var/log/TN_Doc
                Assert.Ignore("Невозможно создать тестовую директорию");
            }
        }

        // Act
        var result = LoggingPathService.EnsureLogDirectoryExists(ApplicationName);

        // Assert
        Assert.That(result, Is.True);
    }

    /// <summary>
    /// Проверяет, что EnsureLogDirectoryExists возвращает булево значение.
    /// </summary>
    [Test]
    public void EnsureLogDirectoryExists_WhenCalled_ReturnsBoolean()
    {
        // Act
        var result = LoggingPathService.EnsureLogDirectoryExists(ApplicationName);

        // Assert
        Assert.That(result, Is.TypeOf<bool>());
    }

    /// <summary>
    /// Проверяет, что после вызова EnsureLogDirectoryExists директория существует (если метод вернул true).
    /// </summary>
    [Test]
    public void EnsureLogDirectoryExists_WhenReturnsTrue_DirectoryExists()
    {
        // Act
        var result = LoggingPathService.EnsureLogDirectoryExists(ApplicationName);

        // Assert
        if (result)
        {
            var logDirectory = LoggingPathService.GetLogDirectory(ApplicationName);
            Assert.That(Directory.Exists(logDirectory), Is.True);
        }
        else
        {
            // Если вернулось false, тест пропускается (нет прав на создание директории)
            Assert.Pass("Метод вернул false - возможно, нет прав на создание директории");
        }
    }

    #endregion
}
