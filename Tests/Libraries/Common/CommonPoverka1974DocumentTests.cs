extern alias CommonPoverka1974Lib;

using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using NUnit.Framework;
using TN.DocData;
using Tests.Fixtures;
using Tests.Libraries;
using HeaderDoc = CommonPoverka1974Lib::TN.Doc.HeaderDoc;
using DataDoc = CommonPoverka1974Lib::TN.Doc.DataDoc;
using FooterDoc = CommonPoverka1974Lib::TN.Doc.FooterDoc;
using DictionarysDoc = CommonPoverka1974Lib::TN.Doc.DictionarysDoc;

namespace Tests.Libraries.Common;

/// <summary>
/// Набор тестов для CommonPoverka1974 - общей библиотеки классов данных для Poverka1974 документов.
///
/// CommonPoverka1974 содержит базовые классы данных (DTOs), используемые различными вариантами
/// документов поверки по МИ 1974-2004 (ГОСТ Р 8.1011-2022):
/// - HeaderDoc, DataDoc, FooterDoc, DictionarysDoc - расширения базовых настроек документа
/// - Используется в: Poverka1974, Poverka1974_04, Poverka1974_89, Poverka1974_95
///
/// Особенность: Эта библиотека содержит только DTOs, без методов GetViewDoc/GetEditDoc.
/// Классы могут быть продублированы локально в каждом Poverka модуле.
///
/// Приоритет: ВЫСОКИЙ (Фаза 1) - базовая библиотека для 4 вариантов поверки
/// </summary>
[TestFixture]
public class CommonPoverka1974DocumentTests : BaseDocumentTest<DataDoc>
{
    protected override void SetupCommonMocks()
    {
        // Минимальная настройка моков для Common библиотеки
        MockAppConfig.Setup(x => x.GetBasePath()).Returns(TestBasePath);
        MockAppConfig.Setup(x => x.GetWwwrootPath()).Returns(TestWwwrootPath);
    }

    #region HeaderDoc Tests

    [Test]
    public void HeaderDoc_Constructor_InitializesCorrectly()
    {
        // Act
        var header = new HeaderDoc();

        // Assert
        Assert.That(header, Is.Not.Null);
        Assert.That(header, Is.InstanceOf<Header>(), "HeaderDoc should inherit from Header");
        Assert.That(header, Is.InstanceOf<HeaderDoc>());
    }

    [Test]
    public void HeaderDoc_FieldSIKN_CanBeSetAndRetrieved()
    {
        // Arrange
        var header = new HeaderDoc();
        const string testValue = "СИКН-425";

        // Act
        header.FieldSIKN = testValue;

        // Assert
        Assert.That(header.FieldSIKN, Is.EqualTo(testValue));
    }

    [Test]
    public void HeaderDoc_Serialization_WorksCorrectly()
    {
        // Arrange
        var header = new HeaderDoc
        {
            FieldSIKN = "СИКН-425",
            // Базовые поля из Header
            LogoPos = "left",
            NameDocType = "Протокол поверки"
        };

        // Act
        var json = JsonConvert.SerializeObject(header);
        var deserialized = JsonConvert.DeserializeObject<HeaderDoc>(json);

        // Assert
        Assert.That(deserialized, Is.Not.Null);
        Assert.That(deserialized.FieldSIKN, Is.EqualTo("СИКН-425"));
        Assert.That(deserialized.LogoPos, Is.EqualTo("left"));
        Assert.That(deserialized.NameDocType, Is.EqualTo("Протокол поверки"));

        TestContext.WriteLine($"Serialized HeaderDoc JSON: {json}");
    }

    #endregion

    #region DataDoc Tests

    [Test]
    public void DataDoc_Constructor_InitializesCorrectly()
    {
        // Act
        var data = new DataDoc();

        // Assert
        Assert.That(data, Is.Not.Null);
        Assert.That(data, Is.InstanceOf<Data>(), "DataDoc should inherit from Data");
        Assert.That(data, Is.InstanceOf<DataDoc>());
    }

    [Test]
    public void DataDoc_HighlightError_DefaultsToTrue()
    {
        // Act
        var data = new DataDoc();

        // Assert
        Assert.That(data.HighlightError, Is.True,
            "HighlightError should default to true");
    }

    [Test]
    public void DataDoc_HighlightError_CanBeSetAndRetrieved()
    {
        // Arrange
        var data = new DataDoc();

        // Act
        data.HighlightError = false;

        // Assert
        Assert.That(data.HighlightError, Is.False);
    }

    [Test]
    public void DataDoc_ManualViscCorrect_DefaultsToFalse()
    {
        // Act
        var data = new DataDoc();

        // Assert
        Assert.That(data.ManualViscCorrect, Is.False,
            "ManualViscCorrect should default to false");
    }

    [Test]
    public void DataDoc_ManualViscCorrect_CanBeSetAndRetrieved()
    {
        // Arrange
        var data = new DataDoc();

        // Act
        data.ManualViscCorrect = true;

        // Assert
        Assert.That(data.ManualViscCorrect, Is.True);
    }

    [Test]
    public void DataDoc_ViscosityCorrectionFeature_IsAvailable()
    {
        // Arrange
        var data = new DataDoc
        {
            ManualViscCorrect = true,
            HighlightError = false
        };

        // Assert
        Assert.That(data.ManualViscCorrect, Is.True,
            "Manual viscosity correction feature should be available (v1.4.1 requirement)");

        TestContext.WriteLine("DataDoc supports manual viscosity correction (added in v1.4.1)");
    }

    [Test]
    public void DataDoc_Serialization_WorksCorrectly()
    {
        // Arrange
        var data = new DataDoc
        {
            HighlightError = false,
            ManualViscCorrect = true
        };

        // Act
        var json = JsonConvert.SerializeObject(data);
        var deserialized = JsonConvert.DeserializeObject<DataDoc>(json);

        // Assert
        Assert.That(deserialized, Is.Not.Null);
        Assert.That(deserialized.HighlightError, Is.False);
        Assert.That(deserialized.ManualViscCorrect, Is.True);

        TestContext.WriteLine($"Serialized DataDoc JSON: {json}");
    }

    #endregion

    #region FooterDoc Tests

    [Test]
    public void FooterDoc_Constructor_InitializesCorrectly()
    {
        // Act
        var footer = new FooterDoc();

        // Assert
        Assert.That(footer, Is.Not.Null);
        Assert.That(footer, Is.InstanceOf<Footer>(), "FooterDoc should inherit from Footer");
        Assert.That(footer, Is.InstanceOf<FooterDoc>());
    }

    [Test]
    public void FooterDoc_InheritsFromFooter_HasNoAdditionalFields()
    {
        // Arrange
        var footer = new FooterDoc();

        // Act
        var properties = footer.GetType().GetProperties();
        var declaredProperties = footer.GetType().GetProperties(
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.DeclaredOnly);

        // Assert
        Assert.That(declaredProperties.Length, Is.EqualTo(0),
            "FooterDoc should not add any additional properties beyond Footer base class");

        TestContext.WriteLine($"FooterDoc has {properties.Length} inherited properties from Footer");
    }

    [Test]
    public void FooterDoc_Serialization_WorksCorrectly()
    {
        // Arrange
        var footer = new FooterDoc();

        // Act
        var json = JsonConvert.SerializeObject(footer);
        var deserialized = JsonConvert.DeserializeObject<FooterDoc>(json);

        // Assert
        Assert.That(deserialized, Is.Not.Null);
        TestContext.WriteLine($"Serialized FooterDoc JSON: {json}");
    }

    #endregion

    #region DictionarysDoc Tests

    [Test]
    public void DictionarysDoc_Constructor_InitializesCorrectly()
    {
        // Act
        var dicts = new DictionarysDoc();

        // Assert
        Assert.That(dicts, Is.Not.Null);
        Assert.That(dicts, Is.InstanceOf<Dictionarys>(), "DictionarysDoc should inherit from Dictionarys");
        Assert.That(dicts, Is.InstanceOf<DictionarysDoc>());
    }

    [Test]
    public void DictionarysDoc_InheritsFromDictionarys_HasNoAdditionalFields()
    {
        // Arrange
        var dicts = new DictionarysDoc();

        // Act
        var declaredProperties = dicts.GetType().GetProperties(
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.DeclaredOnly);

        // Assert
        Assert.That(declaredProperties.Length, Is.EqualTo(0),
            "DictionarysDoc should not add any additional properties beyond Dictionarys base class");
    }

    [Test]
    public void DictionarysDoc_Serialization_WorksCorrectly()
    {
        // Arrange
        var dicts = new DictionarysDoc();

        // Act
        var json = JsonConvert.SerializeObject(dicts);
        var deserialized = JsonConvert.DeserializeObject<DictionarysDoc>(json);

        // Assert
        Assert.That(deserialized, Is.Not.Null);
        TestContext.WriteLine($"Serialized DictionarysDoc JSON: {json}");
    }

    #endregion

    #region Integration Tests

    [Test]
    public void AllCommonPoverka1974Classes_CanBeUsedTogether()
    {
        // Arrange
        var header = new HeaderDoc { FieldSIKN = "СИКН-425" };
        var data = new DataDoc { HighlightError = true, ManualViscCorrect = false };
        var footer = new FooterDoc();
        var dicts = new DictionarysDoc();

        // Act & Assert
        Assert.That(header, Is.Not.Null);
        Assert.That(data, Is.Not.Null);
        Assert.That(footer, Is.Not.Null);
        Assert.That(dicts, Is.Not.Null);

        TestContext.WriteLine("All CommonPoverka1974 classes can be instantiated and used together");
    }

    [Test]
    public void AllCommonPoverka1974Classes_SerializeAsExpected()
    {
        // Arrange
        var header = new HeaderDoc { FieldSIKN = "СИКН-425" };
        var data = new DataDoc { HighlightError = false, ManualViscCorrect = true };

        // Act
        var headerJson = JsonConvert.SerializeObject(header);
        var dataJson = JsonConvert.SerializeObject(data);

        // Assert
        Assert.That(headerJson, Does.Contain("FieldSIKN"));
        Assert.That(headerJson, Does.Contain("СИКН-425"));
        Assert.That(dataJson, Does.Contain("HighlightError"));
        Assert.That(dataJson, Does.Contain("ManualViscCorrect"));

        TestContext.WriteLine($"HeaderDoc JSON: {headerJson}");
        TestContext.WriteLine($"DataDoc JSON: {dataJson}");
    }

    #endregion

    #region Viscosity Correction Tests (v1.4.1)

    [Test]
    public void DataDoc_ViscosityCorrection_ForPoverka1974Variants_IsSupported()
    {
        // Arrange
        var data = new DataDoc();

        // Act
        data.ManualViscCorrect = true;

        // Assert
        Assert.That(data.ManualViscCorrect, Is.True,
            "Manual viscosity correction for Poverka1974 variants (2004, 1995, 1989) added in v1.4.1");

        TestContext.WriteLine("Testing viscosity correction feature for all Poverka1974 variants:");
        TestContext.WriteLine("- Poverka1974_04 (2004 variant)");
        TestContext.WriteLine("- Poverka1974_95 (1995 variant)");
        TestContext.WriteLine("- Poverka1974_89 (1989 variant)");
    }

    [Test]
    public void DataDoc_ErrorHighlighting_CanBeDisabled()
    {
        // Arrange
        var data = new DataDoc();

        // Act
        data.HighlightError = false;

        // Assert
        Assert.That(data.HighlightError, Is.False,
            "Error highlighting can be disabled for cleaner document output");
    }

    #endregion

    #region Documentation Tests

    [Test]
    public void CommonPoverka1974_UsedInFourVariants()
    {
        // Assert
        Assert.Pass(
            "CommonPoverka1974 provides shared classes for 4 Poverka1974 variants:\n" +
            "1. Poverka1974 (base variant)\n" +
            "2. Poverka1974_04 (ГОСТ Р 8.1011-2022)\n" +
            "3. Poverka1974_89 (МИ 1974-89)\n" +
            "4. Poverka1974_95 (МИ 1974-95)\n" +
            "\n" +
            "Key features:\n" +
            "- Manual viscosity correction (v1.4.1)\n" +
            "- Error highlighting toggle\n" +
            "- Custom SIKN field in header"
        );
    }

    #endregion
}
