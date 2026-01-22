using NUnit.Framework;
using TN_Doc.Models.Home;

namespace Tests.Models.Home;

/// <summary>
/// Набор тестов для моделей Home (ModelReport, tReport, TableReportList, TableReportData,
/// TableActAndPassportList, TableMeasurementJornalList).
/// </summary>
[TestFixture]
public class HomeModelsTests
{
    #region ModelReport Tests

    /// <summary>
    /// Проверяет, что конструктор ModelReport инициализирует список из пяти типов отчётов.
    /// </summary>
    [Test]
    public void ModelReport_Constructor_InitializesFiveReportTypes()
    {
        // Arrange & Act
        var modelReport = new ModelReport();

        // Assert
        Assert.That(modelReport.tr, Has.Count.EqualTo(5));
    }

    /// <summary>
    /// Проверяет, что конструктор ModelReport создаёт отчёты с корректными именами.
    /// </summary>
    [Test]
    public void ModelReport_Constructor_ContainsCorrectReportNames()
    {
        // Arrange & Act
        var modelReport = new ModelReport();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(modelReport.tr.Select(r => r.Name), Has.Member("Отчет за два часа"));
            Assert.That(modelReport.tr.Select(r => r.Name), Has.Member("Отчет за смену"));
            Assert.That(modelReport.tr.Select(r => r.Name), Has.Member("Отчет за сутки"));
            Assert.That(modelReport.tr.Select(r => r.Name), Has.Member("Отчет за месяц"));
            Assert.That(modelReport.tr.Select(r => r.Name), Has.Member("Отчет за время ведения ТКО"));
        });
    }

    /// <summary>
    /// Проверяет, что первый элемент списка отчётов имеет id равный 1.
    /// </summary>
    [Test]
    public void ModelReport_tr_FirstItem_HasIdOne()
    {
        // Arrange & Act
        var modelReport = new ModelReport();

        // Assert
        Assert.That(modelReport.tr[0].id, Is.EqualTo(1));
    }

    #endregion

    #region tReport Tests

    /// <summary>
    /// Проверяет, что структура tReport по умолчанию имеет значения по умолчанию.
    /// </summary>
    [Test]
    public void tReport_DefaultValues_AreDefault()
    {
        // Arrange & Act
        var report = new tReport();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(report.id, Is.EqualTo(default(int)));
            Assert.That(report.Name, Is.Null);
            Assert.That(report.ShortName, Is.Null);
        });
    }

    /// <summary>
    /// Проверяет, что структура tReport корректно возвращает установленные значения.
    /// </summary>
    [Test]
    public void tReport_WhenSet_ReturnsCorrectValues()
    {
        // Arrange
        var report = new tReport
        {
            id = 42,
            Name = "Тестовый отчёт",
            ShortName = "Тест"
        };

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(report.id, Is.EqualTo(42));
            Assert.That(report.Name, Is.EqualTo("Тестовый отчёт"));
            Assert.That(report.ShortName, Is.EqualTo("Тест"));
        });
    }

    #endregion

    #region TableReportList Tests

    /// <summary>
    /// Проверяет, что конструктор TableReportList инициализирует свойства значениями по умолчанию.
    /// </summary>
    [Test]
    public void TableReportList_Constructor_InitializesDefaults()
    {
        // Arrange & Act
        var tableReportList = new TableReportList();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(tableReportList.id, Is.EqualTo(default(int)));
            Assert.That(tableReportList.strBegin, Is.Null);
            Assert.That(tableReportList.Begin, Is.EqualTo(default(int)));
            Assert.That(tableReportList.strEnd, Is.Null);
            Assert.That(tableReportList.End, Is.EqualTo(default(int)));
            Assert.That(tableReportList.ReportType, Is.EqualTo(default(int)));
            Assert.That(tableReportList.ReportPeriod, Is.EqualTo(default(int)));
            Assert.That(tableReportList.BIK_ID, Is.EqualTo(default(int)));
            Assert.That(tableReportList.strReportType, Is.Null);
            Assert.That(tableReportList.strDT, Is.Null);
            Assert.That(tableReportList.Data, Is.Null);
        });
    }

    /// <summary>
    /// Проверяет, что свойства TableReportList можно установить и получить корректные значения.
    /// </summary>
    [Test]
    public void TableReportList_Properties_CanBeSet()
    {
        // Arrange
        var data = new TableReportData { id = 100 };
        var tableReportList = new TableReportList
        {
            id = 1,
            strBegin = "2024-01-01 00:00:00",
            Begin = 1704067200,
            strEnd = "2024-01-31 23:59:59",
            End = 1706745599,
            ReportType = 2,
            ReportPeriod = 3,
            BIK_ID = 10,
            strReportType = "Отчет за смену",
            strDT = "01.01.2024 - 31.01.2024",
            Data = data
        };

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(tableReportList.id, Is.EqualTo(1));
            Assert.That(tableReportList.strBegin, Is.EqualTo("2024-01-01 00:00:00"));
            Assert.That(tableReportList.Begin, Is.EqualTo(1704067200));
            Assert.That(tableReportList.strEnd, Is.EqualTo("2024-01-31 23:59:59"));
            Assert.That(tableReportList.End, Is.EqualTo(1706745599));
            Assert.That(tableReportList.ReportType, Is.EqualTo(2));
            Assert.That(tableReportList.ReportPeriod, Is.EqualTo(3));
            Assert.That(tableReportList.BIK_ID, Is.EqualTo(10));
            Assert.That(tableReportList.strReportType, Is.EqualTo("Отчет за смену"));
            Assert.That(tableReportList.strDT, Is.EqualTo("01.01.2024 - 31.01.2024"));
            Assert.That(tableReportList.Data, Is.SameAs(data));
        });
    }

    #endregion

    #region TableReportData Tests

    /// <summary>
    /// Проверяет, что конструктор TableReportData инициализирует свойства значениями по умолчанию.
    /// </summary>
    [Test]
    public void TableReportData_Constructor_InitializesDefaults()
    {
        // Arrange & Act
        var tableReportData = new TableReportData();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(tableReportData.id, Is.EqualTo(default(int)));
            Assert.That(tableReportData.Report, Is.Null);
            Assert.That(tableReportData.ReportRaw, Is.Null);
            Assert.That(tableReportData.DataARM, Is.Null);
        });
    }

    #endregion

    #region TableActAndPassportList Tests

    /// <summary>
    /// Проверяет, что конструктор TableActAndPassportList инициализирует свойства значениями по умолчанию.
    /// </summary>
    [Test]
    public void TableActAndPassportList_Constructor_InitializesDefaults()
    {
        // Arrange & Act
        var tableActAndPassportList = new TableActAndPassportList();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(tableActAndPassportList.id, Is.EqualTo(default(int)));
            Assert.That(tableActAndPassportList.strBegin, Is.Null);
            Assert.That(tableActAndPassportList.Begin, Is.EqualTo(default(int)));
            Assert.That(tableActAndPassportList.strEnd, Is.Null);
            Assert.That(tableActAndPassportList.End, Is.EqualTo(default(int)));
            Assert.That(tableActAndPassportList.PeriodType, Is.EqualTo(default(int)));
            Assert.That(tableActAndPassportList.Period, Is.EqualTo(default(int)));
            Assert.That(tableActAndPassportList.BIK_ID, Is.EqualTo(default(int)));
            Assert.That(tableActAndPassportList.IsFilled, Is.EqualTo(default(int)));
            Assert.That(tableActAndPassportList.TimeStamp, Is.EqualTo(default(long)));
            Assert.That(tableActAndPassportList.strPeriodType, Is.Null);
            Assert.That(tableActAndPassportList.strDT, Is.Null);
            Assert.That(tableActAndPassportList.Data, Is.Null);
        });
    }

    #endregion

    #region TableMeasurementJornalList Tests

    /// <summary>
    /// Проверяет, что конструктор TableMeasurementJornalList инициализирует свойства значениями по умолчанию.
    /// </summary>
    [Test]
    public void TableMeasurementJornalList_Constructor_InitializesDefaults()
    {
        // Arrange & Act
        var tableMeasurementJornalList = new TableMeasurementJornalList();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(tableMeasurementJornalList.id, Is.EqualTo(default(int)));
            Assert.That(tableMeasurementJornalList.Day, Is.EqualTo(default(int)));
            Assert.That(tableMeasurementJornalList.Month, Is.EqualTo(default(int)));
            Assert.That(tableMeasurementJornalList.Year, Is.EqualTo(default(int)));
            Assert.That(tableMeasurementJornalList.BIK_ID, Is.EqualTo(default(int)));
            Assert.That(tableMeasurementJornalList.Data, Is.Null);
        });
    }

    #endregion
}
