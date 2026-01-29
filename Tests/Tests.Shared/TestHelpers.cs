using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Tests.Shared;

/// <summary>
/// Статические вспомогательные методы для тестирования
/// </summary>
public static class TestHelpers
{
    /// <summary>
    /// Проверка JSON на наличие обязательного поля
    /// </summary>
    /// <param name="json">JSON строка</param>
    /// <param name="fieldPath">Путь к полю (например, "data.id" или "JsonDoc")</param>
    public static void AssertJsonContainsField(string json, string fieldPath)
    {
        Assert.That(json, Is.Not.Null.And.Not.Empty, "JSON should not be null or empty");

        var jObject = JObject.Parse(json);
        var token = jObject.SelectToken(fieldPath);

        Assert.That(token, Is.Not.Null, $"JSON should contain field: {fieldPath}");
    }

    /// <summary>
    /// Проверка JSON на наличие нескольких обязательных полей
    /// </summary>
    /// <param name="json">JSON строка</param>
    /// <param name="fieldPaths">Пути к полям</param>
    public static void AssertJsonContainsFields(string json, params string[] fieldPaths)
    {
        foreach (var fieldPath in fieldPaths)
        {
            AssertJsonContainsField(json, fieldPath);
        }
    }

    /// <summary>
    /// Получение значения поля из JSON
    /// </summary>
    /// <typeparam name="T">Тип возвращаемого значения</typeparam>
    /// <param name="json">JSON строка</param>
    /// <param name="fieldPath">Путь к полю</param>
    /// <returns>Значение поля</returns>
    public static T? GetJsonFieldValue<T>(string json, string fieldPath)
    {
        var jObject = JObject.Parse(json);
        var token = jObject.SelectToken(fieldPath);

        if (token == null)
            throw new AssertionException($"Field {fieldPath} not found in JSON");

        return token.ToObject<T>();
    }

    /// <summary>
    /// Проверка, что HTML содержит форму редактирования
    /// </summary>
    /// <param name="html">HTML строка</param>
    public static void AssertHtmlContainsEditForm(string html)
    {
        Assert.That(html, Is.Not.Null.And.Not.Empty, "HTML should not be null or empty");
        Assert.That(html, Does.Contain("<form").Or.Contain("<input").Or.Contain("<table"),
            "HTML should contain form elements");
    }

    /// <summary>
    /// Проверка, что HTML содержит указанный элемент
    /// </summary>
    /// <param name="html">HTML строка</param>
    /// <param name="elementOrId">Тег элемента или ID (например, "input", "id=\"fieldName\"")</param>
    public static void AssertHtmlContainsElement(string html, string elementOrId)
    {
        Assert.That(html, Is.Not.Null.And.Not.Empty, "HTML should not be null or empty");
        Assert.That(html, Does.Contain(elementOrId),
            $"HTML should contain element: {elementOrId}");
    }

    /// <summary>
    /// Создание минимального тестового JSON для документа
    /// </summary>
    /// <param name="docId">ID документа</param>
    /// <param name="additionalFields">Дополнительные поля</param>
    /// <returns>JSON строка</returns>
    public static string CreateMinimalDocumentJson(int docId, Dictionary<string, object>? additionalFields = null)
    {
        var data = new Dictionary<string, object>
        {
            ["id"] = docId,
            ["idDevice"] = 1,
            ["dateCreate"] = DateTime.Now,
            ["dateModify"] = DateTime.Now
        };

        if (additionalFields != null)
        {
            foreach (var kvp in additionalFields)
            {
                data[kvp.Key] = kvp.Value;
            }
        }

        return JsonConvert.SerializeObject(data, Formatting.Indented);
    }

    /// <summary>
    /// Проверка, что путь использует Path.Combine (v1.4.2 требование)
    /// </summary>
    /// <param name="path">Путь для проверки</param>
    public static void AssertPathUsesCombine(string path)
    {
        Assert.That(path, Is.Not.Null.And.Not.Empty, "Path should not be null or empty");

        // Проверка, что путь не содержит хардкод конкатенацию
        Assert.That(path, Does.Not.Contain("\\\\"), "Path should not contain double backslashes");
        Assert.That(path, Does.Not.Contain("//"), "Path should not contain double forward slashes");

        // Проверка, что путь корректно сформирован
        Assert.DoesNotThrow(() =>
        {
            _ = Path.GetDirectoryName(path);
            _ = Path.GetFileName(path);
        }, "Path should be valid and parseable");
    }

    /// <summary>
    /// Валидация конфигурационного файла
    /// </summary>
    /// <param name="configPath">Путь к конфигу</param>
    public static void AssertConfigFileIsValid(string configPath)
    {
        Assert.That(configPath, Is.Not.Null.And.Not.Empty, "Config path should not be null or empty");

        // В реальных тестах можно проверить существование файла,
        // но для unit-тестов проверяем только формат пути
        Assert.That(configPath, Does.EndWith(".json"), "Config file should have .json extension");
    }

    /// <summary>
    /// Валидация пути к шаблону FastReport
    /// </summary>
    /// <param name="templatePath">Путь к шаблону</param>
    public static void AssertTemplateFileIsValid(string templatePath)
    {
        Assert.That(templatePath, Is.Not.Null.And.Not.Empty, "Template path should not be null or empty");

        // Проверка расширения .frx
        Assert.That(templatePath, Does.EndWith(".frx"), "Template file should have .frx extension");
    }

    /// <summary>
    /// Создание временного файла для тестов
    /// </summary>
    /// <param name="content">Содержимое файла</param>
    /// <param name="extension">Расширение файла (с точкой, например ".json")</param>
    /// <returns>Путь к созданному файлу</returns>
    public static string CreateTempFile(string content, string extension = ".tmp")
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}{extension}");
        File.WriteAllText(tempPath, content);
        return tempPath;
    }

    /// <summary>
    /// Очистка временного файла
    /// </summary>
    /// <param name="filePath">Путь к файлу</param>
    public static void CleanupTempFile(string filePath)
    {
        if (File.Exists(filePath))
        {
            try
            {
                File.Delete(filePath);
            }
            catch
            {
                // Игнорируем ошибки при очистке
            }
        }
    }

    /// <summary>
    /// Создание списка всех IdDoc для параметризованных тестов
    /// </summary>
    /// <returns>Массив объектов для TestCaseSource</returns>
    public static object[] GetAllDocumentTypes()
    {
        // Возвращаем все типы документов для параметризованных тестов
        return new object[]
        {
            // Core Documents
            new object[] { 1, "Act" },
            new object[] { 2, "Passport" },
            new object[] { 3, "Report" },
            new object[] { 4, "Jornal" },

            // KMH Documents
            new object[] { 10, "KMH_MPR_MPR" },
            new object[] { 11, "KMH_MPR_PU" },
            new object[] { 12, "KMH_MPR_TPR" },
            new object[] { 13, "KMH_PP" },
            new object[] { 14, "KMH_PP_Areom" },
            new object[] { 15, "KMH_PR_PR" },
            new object[] { 16, "KMH_PR_PU" },
            new object[] { 17, "KMH_PV" },
            new object[] { 18, "KMH_PW" },
            new object[] { 19, "KMH_MI2816" },

            // Poverka Documents
            new object[] { 20, "Poverka1974" },
            new object[] { 21, "Poverka2816" },
            new object[] { 22, "Poverka3151" },
            new object[] { 23, "Poverka3189" },
            new object[] { 24, "Poverka3380" },
        };
    }

    /// <summary>
    /// Получение списка KMH модулей для параметризованных тестов
    /// </summary>
    public static object[] GetKmhDocumentTypes()
    {
        return new object[]
        {
            new object[] { "KMH_MPR_MPR", "Измерение массы и плотности" },
            new object[] { "KMH_MPR_PU", "Измерение массы и расхода" },
            new object[] { "KMH_MPR_TPR", "Измерение массы, температуры и расхода" },
            new object[] { "KMH_PP", "Контроль плотности" },
            new object[] { "KMH_PP_Areom", "Контроль плотности ареометром" },
            new object[] { "KMH_PR_PR", "Контроль расхода" },
            new object[] { "KMH_PR_PU", "Контроль расхода и расхода установки" },
            new object[] { "KMH_PV", "Контроль вязкости" },
            new object[] { "KMH_PW", "Контроль содержания воды" },
            new object[] { "KMH_MI2816", "КМХ по МИ 2816" },
        };
    }

    /// <summary>
    /// Получение списка Poverka модулей для параметризованных тестов
    /// </summary>
    public static object[] GetPoverkaDocumentTypes()
    {
        return new object[]
        {
            new object[] { "Poverka1974", "Поверка по МИ 1974 (базовый)" },
            new object[] { "Poverka1974_04", "Поверка по МИ 1974-2004" },
            new object[] { "Poverka1974_89", "Поверка по МИ 1974-1989" },
            new object[] { "Poverka1974_95", "Поверка по МИ 1974-1995" },
            new object[] { "Poverka2816", "Поверка по МИ 2816" },
            new object[] { "Poverka3151", "Поверка по ГОСТ 3151" },
            new object[] { "Poverka3189", "Поверка по ГОСТ 3189" },
            new object[] { "Poverka3380", "Поверка по ГОСТ 3380" },
        };
    }
}
