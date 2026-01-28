using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Tests.Shared.Fixtures;

/// <summary>
/// Генератор тестовых данных для тестов
/// </summary>
public static class TestDataFixture
{
    /// <summary>
    /// Создание минимального JSON для паспорта качества
    /// </summary>
    public static string CreatePassportJson(int id = 1, int idDevice = 1)
    {
        var data = new
        {
            id,
            idDevice,
            number = $"TEST-{id}",
            dateCreate = DateTime.Now,
            dateStart = DateTime.Now.AddDays(-1),
            dateEnd = DateTime.Now,
            productName = "Нефть сырая",
            volume = 1000.0,
            mass = 850.0,
            density = 0.850,
            parameters = new[]
            {
                new { name = "Плотность", value = "850", units = "кг/м³" },
                new { name = "Вязкость", value = "15.5", units = "мм²/с" }
            }
        };

        return JsonConvert.SerializeObject(data, Formatting.Indented);
    }

    /// <summary>
    /// Создание минимального JSON для акта приема-сдачи
    /// </summary>
    public static string CreateActJson(int id = 1, int idDevice = 1)
    {
        var data = new
        {
            id,
            idDevice,
            number = $"ACT-{id}",
            dateCreate = DateTime.Now,
            dateFrom = DateTime.Now.AddDays(-1),
            dateTo = DateTime.Now,
            shifts = new[]
            {
                new
                {
                    shiftNumber = 1,
                    dateStart = DateTime.Now.AddDays(-1),
                    dateEnd = DateTime.Now.AddDays(-1).AddHours(12),
                    volume = 500.0,
                    mass = 425.0
                }
            },
            totalVolume = 500.0,
            totalMass = 425.0
        };

        return JsonConvert.SerializeObject(data, Formatting.Indented);
    }

    /// <summary>
    /// Создание минимального JSON для KMH документа
    /// </summary>
    public static string CreateKmhJson(int id = 1, int idDevice = 1, string measurementType = "MPR_MPR")
    {
        var data = new
        {
            id,
            idDevice,
            number = $"KMH-{measurementType}-{id}",
            dateCreate = DateTime.Now,
            dateStart = DateTime.Now.AddDays(-1),
            dateEnd = DateTime.Now,
            measurementType,
            measurements = new[]
            {
                new
                {
                    measurementNumber = 1,
                    timestamp = DateTime.Now.AddDays(-1),
                    value = 850.5,
                    uncertainty = 0.5,
                    result = "Годен"
                }
            }
        };

        return JsonConvert.SerializeObject(data, Formatting.Indented);
    }

    /// <summary>
    /// Создание минимального JSON для Poverka документа
    /// </summary>
    public static string CreatePoverkaJson(int id = 1, int idDevice = 1, string poverkaType = "1974")
    {
        var data = new
        {
            id,
            idDevice,
            number = $"POVERKA-{poverkaType}-{id}",
            dateCreate = DateTime.Now,
            datePoverka = DateTime.Now,
            poverkaType,
            siType = "Расходомер",
            siNumber = $"TEST-SI-{id}",
            measurements = new[]
            {
                new
                {
                    pointNumber = 1,
                    nominalValue = 100.0,
                    measuredValue = 100.2,
                    deviation = 0.2,
                    relativeDeviation = 0.2,
                    result = "Годен"
                }
            },
            conclusion = "Средство измерений признано пригодным",
            nextPoverkaDate = DateTime.Now.AddYears(1)
        };

        return JsonConvert.SerializeObject(data, Formatting.Indented);
    }

    /// <summary>
    /// Создание минимального JSON для Отчета
    /// </summary>
    public static string CreateReportJson(int id = 1, int idDevice = 1)
    {
        var data = new
        {
            id,
            idDevice,
            number = $"REPORT-{id}",
            dateCreate = DateTime.Now,
            dateFrom = DateTime.Now.AddDays(-30),
            dateTo = DateTime.Now,
            reportType = "Месячный отчет",
            sections = new[]
            {
                new
                {
                    sectionName = "Общие данные",
                    data = new Dictionary<string, object>
                    {
                        ["totalVolume"] = 15000.0,
                        ["totalMass"] = 12750.0,
                        ["averageDensity"] = 850.0
                    }
                }
            }
        };

        return JsonConvert.SerializeObject(data, Formatting.Indented);
    }

    /// <summary>
    /// Создание минимального JSON для Журнала
    /// </summary>
    public static string CreateJornalJson(int id = 1, int idDevice = 1)
    {
        var data = new
        {
            id,
            idDevice,
            number = $"JORNAL-{id}",
            dateCreate = DateTime.Now,
            jornalType = "Журнал СИ",
            entries = new[]
            {
                new
                {
                    entryNumber = 1,
                    timestamp = DateTime.Now.AddDays(-1),
                    description = "Плановая поверка расходомера",
                    executor = "Иванов И.И.",
                    result = "Выполнено"
                }
            }
        };

        return JsonConvert.SerializeObject(data, Formatting.Indented);
    }

    /// <summary>
    /// Создание минимального HTML для формы редактирования
    /// </summary>
    public static string CreateMinimalEditFormHtml(string documentType, int id)
    {
        return $@"<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"" />
    <title>Редактирование {documentType} #{id}</title>
</head>
<body>
    <form id=""editForm"" method=""post"">
        <input type=""hidden"" name=""id"" value=""{id}"" />
        <div class=""form-group"">
            <label for=""number"">Номер документа:</label>
            <input type=""text"" id=""number"" name=""number"" class=""form-control"" />
        </div>
        <div class=""form-group"">
            <label for=""dateCreate"">Дата создания:</label>
            <input type=""date"" id=""dateCreate"" name=""dateCreate"" class=""form-control"" />
        </div>
        <div class=""form-actions"">
            <button type=""submit"" class=""btn btn-primary"">Сохранить</button>
            <button type=""button"" class=""btn btn-secondary"">Отмена</button>
        </div>
    </form>
</body>
</html>";
    }

    /// <summary>
    /// Получение тестовых данных для различных типов KMH
    /// </summary>
    public static Dictionary<string, object> GetKmhMeasurementData(string kmhType)
    {
        return kmhType switch
        {
            "MPR_MPR" => new Dictionary<string, object>
            {
                ["mass"] = 850.5,
                ["density"] = 850.5,
                ["volume"] = 1.0
            },
            "MPR_PU" => new Dictionary<string, object>
            {
                ["mass"] = 850.5,
                ["flow"] = 100.0,
                ["volume"] = 1.0
            },
            "PR_PR" => new Dictionary<string, object>
            {
                ["flow"] = 100.0,
                ["pressure"] = 1.5,
                ["temperature"] = 20.0
            },
            "PP" => new Dictionary<string, object>
            {
                ["density"] = 850.5,
                ["temperature"] = 20.0
            },
            "PV" => new Dictionary<string, object>
            {
                ["viscosity"] = 15.5,
                ["temperature"] = 20.0
            },
            "PW" => new Dictionary<string, object>
            {
                ["waterContent"] = 0.5,
                ["method"] = "Титрование"
            },
            _ => new Dictionary<string, object>()
        };
    }
}
