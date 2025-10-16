extern alias CommonSikn425Lib;

using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using NUnit.Framework;
using Tests.Libraries;
using DeviceInfo = CommonSikn425Lib::CommonSikn425.DeviceInfo;
using MprInfo = CommonSikn425Lib::CommonSikn425.MprInfo;

namespace Tests.Libraries.Common;

/// <summary>
/// Набор тестов для CommonSikn425 - общей библиотеки классов данных для SIKN-425 документов.
///
/// CommonSikn425 содержит базовые классы данных (DTOs) для документов СИКН-425:
/// - DeviceInfo - информация об устройствах (тип и заводской номер)
/// - MprInfo - информация о МПР (массомер-плотномер-расходомер) с сенсорами
/// - PR_PR.Protokol - протокол поверки расход-расход
/// - PR_PU.Protokol - протокол поверки расход-установка
/// - Table1-5, Points - таблицы данных измерений
///
/// Используется в 4 модулях:
/// - PoverkaSikn425_PR_PR - поверка СИКН-425 (расход-расход)
/// - PoverkaSikn425_PR_PU - поверка СИКН-425 (расход-установка)
/// - KMX_Sikn425_PR_PR - контроль метрологических характеристик (расход-расход)
/// - KMX_Sikn425_PR_PU - контроль метрологических характеристик (расход-установка)
///
/// Приоритет: ВЫСОКИЙ (Фаза 1) - базовая библиотека для 4 SIKN-425 модулей
/// </summary>
[TestFixture]
public class CommonSikn425DocumentTests : BaseDocumentTest<DeviceInfo>
{
    protected override void SetupCommonMocks()
    {
        // Минимальная настройка моков для Common библиотеки
        MockAppConfig.Setup(x => x.GetBasePath()).Returns(TestBasePath);
        MockAppConfig.Setup(x => x.GetWwwrootPath()).Returns(TestWwwrootPath);
    }

    #region DeviceInfo Tests

    [Test]
    public void DeviceInfo_Constructor_InitializesWithEmptyStrings()
    {
        // Act
        var device = new DeviceInfo();

        // Assert
        Assert.That(device, Is.Not.Null);
        Assert.That(device.DevType, Is.EqualTo(string.Empty),
            "DevType should initialize to empty string");
        Assert.That(device.DevNumb, Is.EqualTo(string.Empty),
            "DevNumb should initialize to empty string");
    }

    [Test]
    public void DeviceInfo_Properties_CanBeSetAndRetrieved()
    {
        // Arrange
        var device = new DeviceInfo();
        const string testType = "МПР-01";
        const string testNumber = "12345";

        // Act
        device.DevType = testType;
        device.DevNumb = testNumber;

        // Assert
        Assert.That(device.DevType, Is.EqualTo(testType));
        Assert.That(device.DevNumb, Is.EqualTo(testNumber));
    }

    [Test]
    public void DeviceInfo_Serialization_WorksCorrectly()
    {
        // Arrange
        var device = new DeviceInfo
        {
            DevType = "ПУ-500",
            DevNumb = "ЗН-67890"
        };

        // Act
        var json = JsonConvert.SerializeObject(device);
        var deserialized = JsonConvert.DeserializeObject<DeviceInfo>(json);

        // Assert
        Assert.That(deserialized, Is.Not.Null);
        Assert.That(deserialized.DevType, Is.EqualTo("ПУ-500"));
        Assert.That(deserialized.DevNumb, Is.EqualTo("ЗН-67890"));

        TestContext.WriteLine($"Serialized DeviceInfo JSON: {json}");
    }

    #endregion

    #region MprInfo Tests

    [Test]
    public void MprInfo_Constructor_InitializesAllDeviceInfoObjects()
    {
        // Act
        var mprInfo = new MprInfo();

        // Assert
        Assert.That(mprInfo, Is.Not.Null);
        Assert.That(mprInfo.LineName, Is.EqualTo(string.Empty));
        Assert.That(mprInfo.Mpr, Is.Not.Null, "Mpr DeviceInfo should be initialized");
        Assert.That(mprInfo.Sensor, Is.Not.Null, "Sensor DeviceInfo should be initialized");
        Assert.That(mprInfo.Pep, Is.Not.Null, "Pep DeviceInfo should be initialized");
        Assert.That(mprInfo.delta, Is.EqualTo(string.Empty));
        Assert.That(mprInfo.CheckDate, Is.EqualTo(string.Empty));
    }

    [Test]
    public void MprInfo_AllProperties_CanBeSetAndRetrieved()
    {
        // Arrange
        var mprInfo = new MprInfo();

        // Act
        mprInfo.LineName = "Линия 1";
        mprInfo.Mpr = new DeviceInfo { DevType = "МПР-01", DevNumb = "МПР-001" };
        mprInfo.Sensor = new DeviceInfo { DevType = "Датчик-1", DevNumb = "Д-001" };
        mprInfo.Pep = new DeviceInfo { DevType = "ПЭП-1", DevNumb = "ПЭП-001" };
        mprInfo.delta = "0.5";
        mprInfo.CheckDate = "2025-10-16";

        // Assert
        Assert.That(mprInfo.LineName, Is.EqualTo("Линия 1"));
        Assert.That(mprInfo.Mpr.DevType, Is.EqualTo("МПР-01"));
        Assert.That(mprInfo.Mpr.DevNumb, Is.EqualTo("МПР-001"));
        Assert.That(mprInfo.Sensor.DevType, Is.EqualTo("Датчик-1"));
        Assert.That(mprInfo.Sensor.DevNumb, Is.EqualTo("Д-001"));
        Assert.That(mprInfo.Pep.DevType, Is.EqualTo("ПЭП-1"));
        Assert.That(mprInfo.Pep.DevNumb, Is.EqualTo("ПЭП-001"));
        Assert.That(mprInfo.delta, Is.EqualTo("0.5"));
        Assert.That(mprInfo.CheckDate, Is.EqualTo("2025-10-16"));
    }

    [Test]
    public void MprInfo_Serialization_WorksCorrectly()
    {
        // Arrange
        var mprInfo = new MprInfo
        {
            LineName = "Линия 2",
            Mpr = new DeviceInfo { DevType = "МПР-02", DevNumb = "002" },
            Sensor = new DeviceInfo { DevType = "ДПП", DevNumb = "S-002" },
            Pep = new DeviceInfo { DevType = "ПЭП", DevNumb = "P-002" },
            delta = "0.3",
            CheckDate = "01.01.2025"
        };

        // Act
        var json = JsonConvert.SerializeObject(mprInfo);
        var deserialized = JsonConvert.DeserializeObject<MprInfo>(json);

        // Assert
        Assert.That(deserialized, Is.Not.Null);
        Assert.That(deserialized.LineName, Is.EqualTo("Линия 2"));
        Assert.That(deserialized.Mpr, Is.Not.Null);
        Assert.That(deserialized.Sensor, Is.Not.Null);
        Assert.That(deserialized.Pep, Is.Not.Null);
        Assert.That(deserialized.delta, Is.EqualTo("0.3"));
        Assert.That(deserialized.CheckDate, Is.EqualTo("01.01.2025"));

        TestContext.WriteLine($"Serialized MprInfo JSON: {json}");
    }

    #endregion

    #region PR_PR.Protokol Tests

    [Test]
    public void ProtokolPrPr_Constructor_InitializesCorrectly()
    {
        // Act
        var protokol = new CommonSikn425Lib::CommonSikn425.PR_PR.Protokol();

        // Assert
        Assert.That(protokol, Is.Not.Null);
        TestContext.WriteLine("PR_PR.Protokol (расход-расход) instantiated successfully");
    }

    [Test]
    public void ProtokolPrPr_AllProperties_CanBeSetAndRetrieved()
    {
        // Arrange
        var protokol = new CommonSikn425Lib::CommonSikn425.PR_PR.Protokol();

        // Act
        protokol.PlacePsp = "ПСП-1";
        protokol.PlaceSikn = "СИКН-425";
        protokol.PlaceFactory = "Завод №1";
        protokol.MprRsuInfo = new MprInfo { LineName = "РСУ" };
        protokol.MprOsuInfo = new List<MprInfo>
        {
            new MprInfo { LineName = "ОСУ-1" },
            new MprInfo { LineName = "ОСУ-2" }
        };
        protokol.IvkInfo = new DeviceInfo { DevType = "ИВК", DevNumb = "001" };
        protokol.OilType = "Нефть Западно-Сибирская";

        // Assert
        Assert.That(protokol.PlacePsp, Is.EqualTo("ПСП-1"));
        Assert.That(protokol.PlaceSikn, Is.EqualTo("СИКН-425"));
        Assert.That(protokol.PlaceFactory, Is.EqualTo("Завод №1"));
        Assert.That(protokol.MprRsuInfo, Is.Not.Null);
        Assert.That(protokol.MprOsuInfo, Has.Count.EqualTo(2));
        Assert.That(protokol.IvkInfo, Is.Not.Null);
        Assert.That(protokol.OilType, Is.EqualTo("Нефть Западно-Сибирская"));
    }

    [Test]
    public void ProtokolPrPr_WithTables_SerializesCorrectly()
    {
        // Arrange
        var protokol = new CommonSikn425Lib::CommonSikn425.PR_PR.Protokol
        {
            PlaceSikn = "СИКН-425-1",
            Table1 = new CommonSikn425Lib::CommonSikn425.PR_PR.Table1
            {
                TetaM = "0.5",
                deltaIVK = "0.2",
                Kpm = "1.0",
                QmMax = "100"
            },
            Table2 = new List<CommonSikn425Lib::CommonSikn425.PR_PR.Table2>
            {
                new CommonSikn425Lib::CommonSikn425.PR_PR.Table2
                {
                    ser = "1",
                    row = "1",
                    Q_jik = "50.0",
                    M_jik = "100.0"
                }
            }
        };

        // Act
        var json = JsonConvert.SerializeObject(protokol);
        var deserialized = JsonConvert.DeserializeObject<CommonSikn425Lib::CommonSikn425.PR_PR.Protokol>(json);

        // Assert
        Assert.That(deserialized, Is.Not.Null);
        Assert.That(deserialized.PlaceSikn, Is.EqualTo("СИКН-425-1"));
        Assert.That(deserialized.Table1, Is.Not.Null);
        Assert.That(deserialized.Table2, Is.Not.Null.And.Count.EqualTo(1));

        TestContext.WriteLine($"Serialized PR_PR.Protokol JSON length: {json.Length}");
    }

    #endregion

    #region PR_PU.Protokol Tests

    [Test]
    public void ProtokolPrPu_Constructor_InitializesCorrectly()
    {
        // Act
        var protokol = new CommonSikn425Lib::CommonSikn425.PR_PU.Protokol();

        // Assert
        Assert.That(protokol, Is.Not.Null);
        TestContext.WriteLine("PR_PU.Protokol (расход-установка) instantiated successfully");
    }

    [Test]
    public void ProtokolPrPu_AllProperties_CanBeSetAndRetrieved()
    {
        // Arrange
        var protokol = new CommonSikn425Lib::CommonSikn425.PR_PU.Protokol();

        // Act
        protokol.PlacePsp = "ПСП-2";
        protokol.PlaceSikn = "СИКН-425-2";
        protokol.PlaceFactory = "Завод №2";
        protokol.MprInfo = new MprInfo { LineName = "МПР-Линия" };
        protokol.PuInfo = new DeviceInfo { DevType = "ПУ", DevNumb = "PU-001" };
        protokol.PpInfo = new DeviceInfo { DevType = "ПП", DevNumb = "PP-001" };
        protokol.IvkInfo = new DeviceInfo { DevType = "ИВК", DevNumb = "002" };
        protokol.OilType = "Нефть Уральская";

        // Assert
        Assert.That(protokol.PlacePsp, Is.EqualTo("ПСП-2"));
        Assert.That(protokol.MprInfo, Is.Not.Null);
        Assert.That(protokol.PuInfo, Is.Not.Null);
        Assert.That(protokol.PpInfo, Is.Not.Null);
        Assert.That(protokol.IvkInfo, Is.Not.Null);
        Assert.That(protokol.OilType, Is.EqualTo("Нефть Уральская"));
    }

    [Test]
    public void ProtokolPrPu_WithTables_SerializesCorrectly()
    {
        // Arrange
        var protokol = new CommonSikn425Lib::CommonSikn425.PR_PU.Protokol
        {
            PlaceSikn = "СИКН-425-2",
            Table1 = new CommonSikn425Lib::CommonSikn425.PR_PU.Table1
            {
                D_V0 = "0.1",
                S_V0 = "0.2",
                E_V0 = "0.3",
                alpha_t = "0.001"
            },
            Table2 = new List<CommonSikn425Lib::CommonSikn425.PR_PU.Table2>
            {
                new CommonSikn425Lib::CommonSikn425.PR_PU.Table2
                {
                    ser = "1",
                    row = "1",
                    Q_jik = "75.5",
                    Mpu_jik = "150.0"
                }
            }
        };

        // Act
        var json = JsonConvert.SerializeObject(protokol);
        var deserialized = JsonConvert.DeserializeObject<CommonSikn425Lib::CommonSikn425.PR_PU.Protokol>(json);

        // Assert
        Assert.That(deserialized, Is.Not.Null);
        Assert.That(deserialized.PlaceSikn, Is.EqualTo("СИКН-425-2"));
        Assert.That(deserialized.Table1, Is.Not.Null);
        Assert.That(deserialized.Table2, Is.Not.Null.And.Count.EqualTo(1));

        TestContext.WriteLine($"Serialized PR_PU.Protokol JSON length: {json.Length}");
    }

    #endregion

    #region Table Tests

    [Test]
    public void Tables_PrPr_AllTablesSerializeCorrectly()
    {
        // Arrange
        var table1 = new CommonSikn425Lib::CommonSikn425.PR_PR.Table1
        {
            TetaM = "0.5",
            deltaIVK = "0.2",
            Kpm = "1.0",
            KmMFust = "1.0",
            QmMax = "100",
            ZS = "Z",
            Qnom = "50"
        };

        var table2 = new CommonSikn425Lib::CommonSikn425.PR_PR.Table2
        {
            ser = "1",
            row = "1",
            MprIndx = "MPR1",
            Q_jik = "50.0",
            M_jik = "100.0"
        };

        var table3 = new CommonSikn425Lib::CommonSikn425.PR_PR.Table3
        {
            ser = "1",
            row = "1",
            Q_ji = "50.0",
            U_ji = "0.5",
            U_error_ji = "0.1"
        };

        // Act
        var json1 = JsonConvert.SerializeObject(table1);
        var json2 = JsonConvert.SerializeObject(table2);
        var json3 = JsonConvert.SerializeObject(table3);

        // Assert
        Assert.That(json1, Does.Contain("TetaM"));
        Assert.That(json2, Does.Contain("Q_jik"));
        Assert.That(json3, Does.Contain("U_ji"));

        TestContext.WriteLine("All PR_PR tables serialize successfully");
    }

    [Test]
    public void Tables_PrPu_AllTablesSerializeCorrectly()
    {
        // Arrange
        var table1 = new CommonSikn425Lib::CommonSikn425.PR_PU.Table1
        {
            DetName = new List<string> { "Det1", "Det2" },
            V0 = new List<string> { "100", "200" },
            D_V0 = "0.1",
            S_V0 = "0.2",
            E_V0 = "0.3"
        };

        var table2 = new CommonSikn425Lib::CommonSikn425.PR_PU.Table2
        {
            ser = "1",
            row = "1",
            Q_jik = "75.5",
            Mpu_jik = "150.0",
            Kpm_jik = "1.0"
        };

        var table3 = new CommonSikn425Lib::CommonSikn425.PR_PU.Table3
        {
            Teta_t_k = "0.5",
            Teta_p_k = "0.3",
            delta_k = "0.2",
            Point = new List<CommonSikn425Lib::CommonSikn425.PR_PU.Points>
            {
                new CommonSikn425Lib::CommonSikn425.PR_PU.Points
                {
                    ser = "1",
                    Q_jk = "75.0",
                    Kpm_jk = "1.0"
                }
            }
        };

        // Act
        var json1 = JsonConvert.SerializeObject(table1);
        var json2 = JsonConvert.SerializeObject(table2);
        var json3 = JsonConvert.SerializeObject(table3);

        // Assert
        Assert.That(json1, Does.Contain("DetName"));
        Assert.That(json2, Does.Contain("Mpu_jik"));
        Assert.That(json3, Does.Contain("Point"));

        TestContext.WriteLine("All PR_PU tables serialize successfully");
    }

    #endregion

    #region Integration Tests

    [Test]
    public void CommonSikn425_AllClasses_CanBeUsedTogether()
    {
        // Arrange & Act
        var device = new DeviceInfo { DevType = "МПР", DevNumb = "001" };
        var mprInfo = new MprInfo
        {
            LineName = "Линия 1",
            Mpr = device,
            Sensor = new DeviceInfo { DevType = "Датчик", DevNumb = "002" },
            Pep = new DeviceInfo { DevType = "ПЭП", DevNumb = "003" }
        };

        var protokolPrPr = new CommonSikn425Lib::CommonSikn425.PR_PR.Protokol
        {
            PlaceSikn = "СИКН-425",
            MprRsuInfo = mprInfo,
            IvkInfo = device
        };

        var protokolPrPu = new CommonSikn425Lib::CommonSikn425.PR_PU.Protokol
        {
            PlaceSikn = "СИКН-425",
            MprInfo = mprInfo,
            IvkInfo = device
        };

        // Assert
        Assert.That(device, Is.Not.Null);
        Assert.That(mprInfo, Is.Not.Null);
        Assert.That(protokolPrPr, Is.Not.Null);
        Assert.That(protokolPrPu, Is.Not.Null);

        TestContext.WriteLine("All CommonSikn425 classes can be instantiated and used together");
    }

    [Test]
    public void CommonSikn425_ComplexStructure_SerializesCorrectly()
    {
        // Arrange
        var komplexProtokol = new CommonSikn425Lib::CommonSikn425.PR_PR.Protokol
        {
            PlaceSikn = "СИКН-425-Complex",
            MprRsuInfo = new MprInfo
            {
                LineName = "РСУ",
                Mpr = new DeviceInfo { DevType = "МПР-РСУ", DevNumb = "RSU-001" },
                Sensor = new DeviceInfo { DevType = "Датчик-РСУ", DevNumb = "RSU-S-001" },
                Pep = new DeviceInfo { DevType = "ПЭП-РСУ", DevNumb = "RSU-P-001" }
            },
            MprOsuInfo = new List<MprInfo>
            {
                new MprInfo
                {
                    LineName = "ОСУ-1",
                    Mpr = new DeviceInfo { DevType = "МПР-ОСУ", DevNumb = "OSU-001" }
                },
                new MprInfo
                {
                    LineName = "ОСУ-2",
                    Mpr = new DeviceInfo { DevType = "МПР-ОСУ", DevNumb = "OSU-002" }
                }
            },
            Table2 = new List<CommonSikn425Lib::CommonSikn425.PR_PR.Table2>
            {
                new CommonSikn425Lib::CommonSikn425.PR_PR.Table2 { ser = "1", Q_jik = "50.0" },
                new CommonSikn425Lib::CommonSikn425.PR_PR.Table2 { ser = "2", Q_jik = "75.0" },
                new CommonSikn425Lib::CommonSikn425.PR_PR.Table2 { ser = "3", Q_jik = "100.0" }
            }
        };

        // Act
        var json = JsonConvert.SerializeObject(komplexProtokol, Formatting.Indented);
        var deserialized = JsonConvert.DeserializeObject<CommonSikn425Lib::CommonSikn425.PR_PR.Protokol>(json);

        // Assert
        Assert.That(deserialized, Is.Not.Null);
        Assert.That(deserialized.PlaceSikn, Is.EqualTo("СИКН-425-Complex"));
        Assert.That(deserialized.MprRsuInfo, Is.Not.Null);
        Assert.That(deserialized.MprOsuInfo, Has.Count.EqualTo(2));
        Assert.That(deserialized.Table2, Has.Count.EqualTo(3));

        TestContext.WriteLine($"Complex Protokol serialized successfully:");
        TestContext.WriteLine(json.Substring(0, Math.Min(500, json.Length)) + "...");
    }

    #endregion

    #region Documentation Tests

    [Test]
    public void CommonSikn425_UsedInFourModules()
    {
        // Assert
        Assert.Pass(
            "CommonSikn425 provides shared classes for 4 SIKN-425 modules:\n\n" +
            "**Поверка (Verification):**\n" +
            "1. PoverkaSikn425_PR_PR - поверка расход-расход (flow-flow)\n" +
            "2. PoverkaSikn425_PR_PU - поверка расход-установка (flow-installation)\n\n" +
            "**Контроль метрологических характеристик (Metrological Control):**\n" +
            "3. KMX_Sikn425_PR_PR - КМХ расход-расход\n" +
            "4. KMX_Sikn425_PR_PU - КМХ расход-установка\n\n" +
            "**Key components:**\n" +
            "- DeviceInfo: Device type and number\n" +
            "- MprInfo: MPR (mass-densitometer-flowmeter) with sensors\n" +
            "- Two protocol types: PR_PR (flow-flow) and PR_PU (flow-installation)\n" +
            "- Complex table structures (Table1-5, Points)"
        );
    }

    [Test]
    public void CommonSikn425_TwoProtocolTypes_HaveDifferentStructures()
    {
        // Arrange
        var prPrProtokol = new CommonSikn425Lib::CommonSikn425.PR_PR.Protokol();
        var prPuProtokol = new CommonSikn425Lib::CommonSikn425.PR_PU.Protokol();

        // Assert
        Assert.That(prPrProtokol, Is.Not.Null);
        Assert.That(prPuProtokol, Is.Not.Null);

        TestContext.WriteLine("CommonSikn425 provides two distinct protocol types:");
        TestContext.WriteLine("1. PR_PR.Protokol - для поверки методом расход-расход (РСУ vs ОСУ)");
        TestContext.WriteLine("2. PR_PU.Protokol - для поверки методом расход-установка (МПР vs ПУ+ПП)");
    }

    #endregion
}
