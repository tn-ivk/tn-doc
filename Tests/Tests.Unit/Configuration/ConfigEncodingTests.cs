using System.Text;
using System.Text.Json;
using NUnit.Framework;

namespace Tests.Configuration;

/// <summary>
/// Тесты проверки кодировки и валидности конфигурационных JSON-файлов.
/// Предотвращают проблемы с некорректной кодировкой (Windows-1251 → UTF-8).
/// </summary>
[TestFixture]
public class ConfigEncodingTests
{
    private string _cfgDirectoryPath = null!;
    private string[] _jsonFiles = Array.Empty<string>();

    /// <summary>
    /// Символ замены Unicode (U+FFFD), появляется при некорректном преобразовании кодировок.
    /// </summary>
    private const char ReplacementCharacter = '\uFFFD';

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _cfgDirectoryPath = FindCfgDirectory();

        if (Directory.Exists(_cfgDirectoryPath))
        {
            _jsonFiles = Directory.GetFiles(_cfgDirectoryPath, "*.json", SearchOption.AllDirectories);
        }
    }

    /// <summary>
    /// Проверяет, что JSON-файлы не содержат символ замены Unicode (U+FFFD).
    /// Этот символ появляется при некорректном преобразовании из Windows-1251 в UTF-8.
    /// </summary>
    [Test]
    public void AllJsonConfigFiles_ShouldNotContainReplacementCharacter()
    {
        // Arrange
        var filesWithProblems = new List<EncodingProblem>();

        // Act
        foreach (var filePath in _jsonFiles)
        {
            var content = File.ReadAllText(filePath, Encoding.UTF8);
            var problems = FindReplacementCharacters(content, filePath);
            filesWithProblems.AddRange(problems);
        }

        // Assert
        if (filesWithProblems.Count > 0)
        {
            var report = GenerateDetailedReport(filesWithProblems, "символ замены Unicode (U+FFFD)");
            Assert.Fail(report);
        }

        Assert.Pass($"Проверено {_jsonFiles.Length} JSON-файлов. Символы замены U+FFFD не обнаружены.");
    }

    /// <summary>
    /// Проверяет, что JSON-файлы содержат валидную UTF-8 кодировку.
    /// Некорректные байтовые последовательности указывают на проблемы с кодировкой.
    /// </summary>
    [Test]
    public void AllJsonConfigFiles_ShouldBeValidUtf8()
    {
        // Arrange
        var filesWithProblems = new List<EncodingProblem>();

        // Act
        foreach (var filePath in _jsonFiles)
        {
            var problems = ValidateUtf8Encoding(filePath);
            filesWithProblems.AddRange(problems);
        }

        // Assert
        if (filesWithProblems.Count > 0)
        {
            var report = GenerateDetailedReport(filesWithProblems, "невалидная UTF-8 последовательность");
            Assert.Fail(report);
        }

        Assert.Pass($"Проверено {_jsonFiles.Length} JSON-файлов. Все файлы содержат валидную UTF-8 кодировку.");
    }

    /// <summary>
    /// Проверяет, что JSON-файлы содержат синтаксически корректный JSON.
    /// </summary>
    [Test]
    public void AllJsonConfigFiles_ShouldBeValidJsonSyntax()
    {
        // Arrange
        var filesWithProblems = new List<EncodingProblem>();

        // Act
        foreach (var filePath in _jsonFiles)
        {
            var problem = ValidateJsonSyntax(filePath);
            if (problem != null)
            {
                filesWithProblems.Add(problem);
            }
        }

        // Assert
        if (filesWithProblems.Count > 0)
        {
            var report = GenerateDetailedReport(filesWithProblems, "невалидный JSON синтаксис");
            Assert.Fail(report);
        }

        Assert.Pass($"Проверено {_jsonFiles.Length} JSON-файлов. Все файлы содержат валидный JSON.");
    }

    #region Helper Methods

    /// <summary>
    /// Находит директорию Cfg относительно директории тестов.
    /// </summary>
    private static string FindCfgDirectory()
    {
        // Начинаем от текущей директории (обычно bin/Debug/net8.0)
        var currentDir = AppContext.BaseDirectory;

        // Поднимаемся вверх по директориям в поисках TN_Doc/Cfg
        var directory = new DirectoryInfo(currentDir);

        while (directory != null)
        {
            var cfgPath = Path.Combine(directory.FullName, "TN_Doc", "Cfg");
            if (Directory.Exists(cfgPath))
            {
                return cfgPath;
            }

            directory = directory.Parent;
        }

        // Fallback: относительный путь от Tests.Unit
        return Path.GetFullPath(Path.Combine(currentDir, "..", "..", "..", "..", "TN_Doc", "Cfg"));
    }

    /// <summary>
    /// Находит все символы замены Unicode в содержимом файла.
    /// </summary>
    private static List<EncodingProblem> FindReplacementCharacters(string content, string filePath)
    {
        var problems = new List<EncodingProblem>();
        var lines = content.Split('\n');

        for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++)
        {
            var line = lines[lineIndex];
            for (int charIndex = 0; charIndex < line.Length; charIndex++)
            {
                if (line[charIndex] == ReplacementCharacter)
                {
                    problems.Add(new EncodingProblem
                    {
                        FilePath = filePath,
                        LineNumber = lineIndex + 1,
                        CharPosition = charIndex + 1,
                        Context = ExtractContext(line, charIndex),
                        ProblemDescription = "Найден символ замены U+FFFD"
                    });
                }
            }
        }

        return problems;
    }

    /// <summary>
    /// Проверяет файл на валидность UTF-8 кодировки.
    /// </summary>
    private static List<EncodingProblem> ValidateUtf8Encoding(string filePath)
    {
        var problems = new List<EncodingProblem>();

        try
        {
            var bytes = File.ReadAllBytes(filePath);

            // Используем DecoderExceptionFallback для обнаружения невалидных последовательностей
            var utf8WithException = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

            try
            {
                utf8WithException.GetString(bytes);
            }
            catch (DecoderFallbackException ex)
            {
                // Находим позицию ошибки
                var validPart = Encoding.UTF8.GetString(bytes, 0, ex.Index);
                var lines = validPart.Split('\n');
                var lineNumber = lines.Length;
                var charPosition = lines.Length > 0 ? lines[^1].Length + 1 : 1;

                problems.Add(new EncodingProblem
                {
                    FilePath = filePath,
                    LineNumber = lineNumber,
                    CharPosition = charPosition,
                    Context = $"Байты: [{string.Join(", ", bytes.Skip(ex.Index).Take(4).Select(b => $"0x{b:X2}"))}]",
                    ProblemDescription = $"Невалидная UTF-8 последовательность на позиции {ex.Index}"
                });
            }
        }
        catch (Exception ex)
        {
            problems.Add(new EncodingProblem
            {
                FilePath = filePath,
                LineNumber = 0,
                CharPosition = 0,
                Context = string.Empty,
                ProblemDescription = $"Ошибка чтения файла: {ex.Message}"
            });
        }

        return problems;
    }

    /// <summary>
    /// Проверяет файл на корректность JSON синтаксиса.
    /// </summary>
    private static EncodingProblem? ValidateJsonSyntax(string filePath)
    {
        try
        {
            var content = File.ReadAllText(filePath, Encoding.UTF8);

            // Используем System.Text.Json для строгой проверки
            using var document = JsonDocument.Parse(content, new JsonDocumentOptions
            {
                AllowTrailingCommas = true,
                CommentHandling = JsonCommentHandling.Skip
            });

            return null;
        }
        catch (JsonException ex)
        {
            return new EncodingProblem
            {
                FilePath = filePath,
                LineNumber = (int)(ex.LineNumber ?? 0) + 1,
                CharPosition = (int)(ex.BytePositionInLine ?? 0) + 1,
                Context = ex.Path ?? string.Empty,
                ProblemDescription = ex.Message
            };
        }
        catch (Exception ex)
        {
            return new EncodingProblem
            {
                FilePath = filePath,
                LineNumber = 0,
                CharPosition = 0,
                Context = string.Empty,
                ProblemDescription = $"Ошибка чтения файла: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Извлекает контекст (окружающий текст) для позиции символа.
    /// </summary>
    private static string ExtractContext(string line, int position)
    {
        const int contextRadius = 20;

        var start = Math.Max(0, position - contextRadius);
        var end = Math.Min(line.Length, position + contextRadius + 1);

        var before = line[start..position];
        var after = position + 1 < line.Length ? line[(position + 1)..end] : string.Empty;

        var prefix = start > 0 ? "..." : string.Empty;
        var suffix = end < line.Length ? "..." : string.Empty;

        return $"{prefix}{before}[U+FFFD]{after}{suffix}";
    }

    /// <summary>
    /// Генерирует детальный отчёт о найденных проблемах.
    /// </summary>
    private static string GenerateDetailedReport(List<EncodingProblem> problems, string problemType)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"\n=== Обнаружены проблемы кодировки ({problemType}) ===\n");
        sb.AppendLine($"Всего проблем: {problems.Count}");
        sb.AppendLine($"Затронуто файлов: {problems.Select(p => p.FilePath).Distinct().Count()}\n");

        var groupedByFile = problems.GroupBy(p => p.FilePath);

        foreach (var fileGroup in groupedByFile)
        {
            var relativePath = GetRelativePath(fileGroup.Key);
            sb.AppendLine($"--- Файл: {relativePath} ---");

            foreach (var problem in fileGroup)
            {
                sb.AppendLine($"  Строка {problem.LineNumber}, позиция {problem.CharPosition}:");
                sb.AppendLine($"    {problem.ProblemDescription}");
                if (!string.IsNullOrEmpty(problem.Context))
                {
                    sb.AppendLine($"    Контекст: {problem.Context}");
                }
            }

            sb.AppendLine();
        }

        return sb.ToString();
    }

    /// <summary>
    /// Получает относительный путь от директории Cfg.
    /// </summary>
    private static string GetRelativePath(string fullPath)
    {
        var cfgIndex = fullPath.IndexOf("Cfg", StringComparison.OrdinalIgnoreCase);
        return cfgIndex >= 0 ? fullPath[cfgIndex..] : Path.GetFileName(fullPath);
    }

    #endregion

    #region Nested Types

    /// <summary>
    /// Информация о проблеме кодировки.
    /// </summary>
    private sealed class EncodingProblem
    {
        public required string FilePath { get; init; }
        public required int LineNumber { get; init; }
        public required int CharPosition { get; init; }
        public required string Context { get; init; }
        public required string ProblemDescription { get; init; }
    }

    #endregion
}
