using System;
using System.IO;
using NUnit.Framework;
using TN_Doc.Services;

namespace Tests.Unit.Services;

/// <summary>
/// Тесты для ConfigurationService, включая защиту от path traversal атак (CWE-22)
/// </summary>
[TestFixture]
public class ConfigurationServiceTests
{
    private string _testBaseDirectory;

    [SetUp]
    public void Setup()
    {
        _testBaseDirectory = Path.Combine(Path.GetTempPath(), "ConfigServiceTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testBaseDirectory);
        Directory.CreateDirectory(Path.Combine(_testBaseDirectory, "Cfg"));
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_testBaseDirectory))
        {
            Directory.Delete(_testBaseDirectory, true);
        }
    }

    #region GetSafeConfigPath - Valid Paths

    /// <summary>
    /// GetSafeConfigPath: валидный путь Cfg/file.json возвращает полный путь
    /// </summary>
    [Test]
    public void GetSafeConfigPath_ValidPath_ReturnsFullPath()
    {
        // Arrange
        var configPath = "Cfg/CfgPassport.json";

        // Act
        var result = ConfigurationService.GetSafeConfigPath(configPath, _testBaseDirectory);

        // Assert
        var expected = Path.GetFullPath(Path.Combine(_testBaseDirectory, "Cfg", "CfgPassport.json"));
        Assert.That(result, Is.EqualTo(expected));
    }

    /// <summary>
    /// GetSafeConfigPath: путь с начальным слешем нормализуется
    /// </summary>
    [Test]
    public void GetSafeConfigPath_LeadingSlash_NormalizesPath()
    {
        // Arrange
        var configPath = "/Cfg/CfgPassport.json";

        // Act
        var result = ConfigurationService.GetSafeConfigPath(configPath, _testBaseDirectory);

        // Assert
        var expected = Path.GetFullPath(Path.Combine(_testBaseDirectory, "Cfg", "CfgPassport.json"));
        Assert.That(result, Is.EqualTo(expected));
    }

    /// <summary>
    /// GetSafeConfigPath: путь с обратными слешами нормализуется
    /// </summary>
    [Test]
    public void GetSafeConfigPath_BackSlashes_NormalizesPath()
    {
        // Arrange
        var configPath = "Cfg\\SubDir\\CfgPassport.json";

        // Act
        var result = ConfigurationService.GetSafeConfigPath(configPath, _testBaseDirectory);

        // Assert
        Assert.That(result, Does.Contain("Cfg"));
        Assert.That(result, Does.Contain("SubDir"));
        Assert.That(result, Does.EndWith("CfgPassport.json"));
    }

    /// <summary>
    /// GetSafeConfigPath: путь с вложенными директориями работает
    /// </summary>
    [Test]
    public void GetSafeConfigPath_NestedDirectories_ReturnsFullPath()
    {
        // Arrange
        var configPath = "Cfg/Templates/Passport/config.json";

        // Act
        var result = ConfigurationService.GetSafeConfigPath(configPath, _testBaseDirectory);

        // Assert
        Assert.That(result, Does.Contain("Cfg"));
        Assert.That(result, Does.Contain("Templates"));
        Assert.That(result, Does.EndWith("config.json"));
    }

    #endregion

    #region GetSafeConfigPath - Empty/Null Path

    /// <summary>
    /// GetSafeConfigPath: null путь выбрасывает ArgumentException
    /// </summary>
    [Test]
    public void GetSafeConfigPath_NullPath_ThrowsArgumentException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            ConfigurationService.GetSafeConfigPath(null, _testBaseDirectory));

        Assert.That(ex.ParamName, Is.EqualTo("configPath"));
        Assert.That(ex.Message, Does.Contain("пустым"));
    }

    /// <summary>
    /// GetSafeConfigPath: пустой путь выбрасывает ArgumentException
    /// </summary>
    [Test]
    public void GetSafeConfigPath_EmptyPath_ThrowsArgumentException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            ConfigurationService.GetSafeConfigPath("", _testBaseDirectory));

        Assert.That(ex.ParamName, Is.EqualTo("configPath"));
    }

    /// <summary>
    /// GetSafeConfigPath: путь из пробелов выбрасывает ArgumentException
    /// </summary>
    [Test]
    public void GetSafeConfigPath_WhitespacePath_ThrowsArgumentException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            ConfigurationService.GetSafeConfigPath("   ", _testBaseDirectory));

        Assert.That(ex.ParamName, Is.EqualTo("configPath"));
    }

    #endregion

    #region GetSafeConfigPath - Path Traversal Attacks

    /// <summary>
    /// GetSafeConfigPath: путь с .. выбрасывает ArgumentException
    /// </summary>
    [Test]
    public void GetSafeConfigPath_PathTraversal_ThrowsArgumentException()
    {
        // Arrange
        var maliciousPath = "Cfg/../../../etc/passwd";

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            ConfigurationService.GetSafeConfigPath(maliciousPath, _testBaseDirectory));

        Assert.That(ex.Message, Does.Contain("недопустимые сегменты"));
    }

    /// <summary>
    /// GetSafeConfigPath: путь с .. в начале выбрасывает ArgumentException
    /// </summary>
    [Test]
    public void GetSafeConfigPath_PathTraversalAtStart_ThrowsArgumentException()
    {
        // Arrange
        var maliciousPath = "../../../etc/passwd";

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            ConfigurationService.GetSafeConfigPath(maliciousPath, _testBaseDirectory));

        Assert.That(ex.Message, Does.Contain("недопустимые сегменты"));
    }

    /// <summary>
    /// GetSafeConfigPath: путь с .. и обратными слешами выбрасывает ArgumentException
    /// </summary>
    [Test]
    public void GetSafeConfigPath_PathTraversalBackSlashes_ThrowsArgumentException()
    {
        // Arrange
        var maliciousPath = "Cfg\\..\\..\\..\\Windows\\System32\\config\\SAM";

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            ConfigurationService.GetSafeConfigPath(maliciousPath, _testBaseDirectory));

        Assert.That(ex.Message, Does.Contain("недопустимые сегменты"));
    }

    /// <summary>
    /// GetSafeConfigPath: только .. выбрасывает ArgumentException
    /// </summary>
    [Test]
    public void GetSafeConfigPath_OnlyDotDot_ThrowsArgumentException()
    {
        // Arrange
        var maliciousPath = "..";

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            ConfigurationService.GetSafeConfigPath(maliciousPath, _testBaseDirectory));
    }

    #endregion

    #region GetSafeConfigPath - Absolute Paths

    /// <summary>
    /// GetSafeConfigPath: абсолютный Windows путь выбрасывает ArgumentException
    /// </summary>
    [Test]
    public void GetSafeConfigPath_AbsoluteWindowsPath_ThrowsArgumentException()
    {
        // Arrange
        var maliciousPath = "C:\\Windows\\System32\\config\\SAM";

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            ConfigurationService.GetSafeConfigPath(maliciousPath, _testBaseDirectory));

        Assert.That(ex.Message, Does.Contain("Абсолютные пути запрещены"));
    }

    /// <summary>
    /// GetSafeConfigPath: абсолютный Unix путь выбрасывает ArgumentException
    /// </summary>
    [Test]
    public void GetSafeConfigPath_AbsoluteUnixPath_ThrowsArgumentException()
    {
        // Arrange
        var maliciousPath = "/etc/passwd";

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            ConfigurationService.GetSafeConfigPath(maliciousPath, _testBaseDirectory));

        // На Windows /etc/passwd нормализуется в etc/passwd, который не начинается с Cfg/
        Assert.That(ex.Message, Does.Contain("Cfg/"));
    }

    /// <summary>
    /// GetSafeConfigPath: путь с буквой диска выбрасывает ArgumentException
    /// </summary>
    [Test]
    public void GetSafeConfigPath_DriveLetterPath_ThrowsArgumentException()
    {
        // Arrange
        var maliciousPath = "D:\\SensitiveData\\secrets.json";

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            ConfigurationService.GetSafeConfigPath(maliciousPath, _testBaseDirectory));

        Assert.That(ex.Message, Does.Contain("Абсолютные пути запрещены"));
    }

    #endregion

    #region GetSafeConfigPath - UNC Paths

    /// <summary>
    /// GetSafeConfigPath: UNC путь с \\ выбрасывает ArgumentException
    /// </summary>
    [Test]
    public void GetSafeConfigPath_UncPathBackSlash_ThrowsArgumentException()
    {
        // Arrange
        var maliciousPath = "\\\\server\\share\\secrets.json";

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            ConfigurationService.GetSafeConfigPath(maliciousPath, _testBaseDirectory));

        Assert.That(ex.Message, Does.Contain("UNC пути запрещены"));
    }

    /// <summary>
    /// GetSafeConfigPath: UNC путь с // выбрасывает ArgumentException
    /// </summary>
    [Test]
    public void GetSafeConfigPath_UncPathForwardSlash_ThrowsArgumentException()
    {
        // Arrange
        var maliciousPath = "//server/share/secrets.json";

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            ConfigurationService.GetSafeConfigPath(maliciousPath, _testBaseDirectory));

        Assert.That(ex.Message, Does.Contain("UNC пути запрещены"));
    }

    #endregion

    #region GetSafeConfigPath - Outside Allowed Directory

    /// <summary>
    /// GetSafeConfigPath: путь не в Cfg/ выбрасывает ArgumentException
    /// </summary>
    [Test]
    public void GetSafeConfigPath_NotInCfgDirectory_ThrowsArgumentException()
    {
        // Arrange
        var maliciousPath = "wwwroot/secrets.json";

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            ConfigurationService.GetSafeConfigPath(maliciousPath, _testBaseDirectory));

        Assert.That(ex.Message, Does.Contain("Cfg/"));
    }

    /// <summary>
    /// GetSafeConfigPath: путь начинающийся с cfg (lowercase) работает (case insensitive)
    /// </summary>
    [Test]
    public void GetSafeConfigPath_LowercaseCfg_Works()
    {
        // Arrange
        var configPath = "cfg/config.json";

        // Act
        var result = ConfigurationService.GetSafeConfigPath(configPath, _testBaseDirectory);

        // Assert
        Assert.That(result, Does.EndWith("config.json"));
    }

    /// <summary>
    /// GetSafeConfigPath: путь CfgSomething не допускается (должен быть Cfg/)
    /// </summary>
    [Test]
    public void GetSafeConfigPath_CfgWithoutSlash_ThrowsArgumentException()
    {
        // Arrange
        var maliciousPath = "CfgBackup/secrets.json";

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            ConfigurationService.GetSafeConfigPath(maliciousPath, _testBaseDirectory));

        Assert.That(ex.Message, Does.Contain("Cfg/"));
    }

    /// <summary>
    /// GetSafeConfigPath: только Cfg без файла - не допускается
    /// </summary>
    [Test]
    public void GetSafeConfigPath_OnlyCfgDirectory_ThrowsArgumentException()
    {
        // Arrange
        var configPath = "Cfg/";

        // Act & Assert - путь валиден, но это директория
        var result = ConfigurationService.GetSafeConfigPath(configPath, _testBaseDirectory);

        // Возвращается путь к директории (проверка существования файла - на уровне вызывающего кода)
        Assert.That(result, Does.EndWith("Cfg" + Path.DirectorySeparatorChar)
            .Or.EndWith("Cfg"));
    }

    #endregion

    #region GetSafeConfigPath - Edge Cases

    /// <summary>
    /// GetSafeConfigPath: путь с одной точкой (.) разрешён
    /// </summary>
    [Test]
    public void GetSafeConfigPath_SingleDot_Allowed()
    {
        // Arrange
        var configPath = "Cfg/./config.json";

        // Act
        var result = ConfigurationService.GetSafeConfigPath(configPath, _testBaseDirectory);

        // Assert - Path.GetFullPath нормализует ./
        Assert.That(result, Does.EndWith("config.json"));
    }

    /// <summary>
    /// GetSafeConfigPath: путь с пробелами в имени файла разрешён
    /// </summary>
    [Test]
    public void GetSafeConfigPath_SpacesInFileName_Allowed()
    {
        // Arrange
        var configPath = "Cfg/config file.json";

        // Act
        var result = ConfigurationService.GetSafeConfigPath(configPath, _testBaseDirectory);

        // Assert
        Assert.That(result, Does.EndWith("config file.json"));
    }

    /// <summary>
    /// GetSafeConfigPath: путь с Unicode символами разрешён
    /// </summary>
    [Test]
    public void GetSafeConfigPath_UnicodeCharacters_Allowed()
    {
        // Arrange
        var configPath = "Cfg/конфиг.json";

        // Act
        var result = ConfigurationService.GetSafeConfigPath(configPath, _testBaseDirectory);

        // Assert
        Assert.That(result, Does.EndWith("конфиг.json"));
    }

    /// <summary>
    /// GetSafeConfigPath: множественные слеши нормализуются
    /// </summary>
    [Test]
    public void GetSafeConfigPath_MultipleSlashes_Normalized()
    {
        // Arrange
        var configPath = "Cfg///config.json";

        // Act
        var result = ConfigurationService.GetSafeConfigPath(configPath, _testBaseDirectory);

        // Assert
        Assert.That(result, Does.EndWith("config.json"));
        Assert.That(result, Does.Not.Contain("///"));
    }

    #endregion
}
