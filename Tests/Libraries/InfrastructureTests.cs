using System;
using System.IO;
using NUnit.Framework;
using Tests.Fixtures;
using Tests.Libraries;

namespace Tests.Libraries;

/// <summary>
/// Тесты для проверки работоспособности тестовой инфраструктуры.
/// Эти тесты не требуют зависимостей от конкретных библиотек документов.
/// </summary>
[TestFixture]
public class InfrastructureTests
{
    [Test]
    public void DocumentTestHelpers_CreateMinimalDocumentJson_ReturnsValidJson()
    {
        // Arrange
        const int testId = 1;

        // Act
        var json = DocumentTestHelpers.CreateMinimalDocumentJson(testId);

        // Assert
        Assert.That(json, Is.Not.Null);
        Assert.That(json, Is.Not.Empty);
        Assert.DoesNotThrow(() => Newtonsoft.Json.JsonConvert.DeserializeObject(json));

        TestContext.WriteLine($"Generated JSON: {json}");
    }

    [Test]
    public void DocumentTestHelpers_AssertJsonContainsField_FindsExistingField()
    {
        // Arrange
        var json = "{\"id\": 1, \"name\": \"Test\"}";

        // Act & Assert
        Assert.DoesNotThrow(() =>
        {
            DocumentTestHelpers.AssertJsonContainsField(json, "id");
            DocumentTestHelpers.AssertJsonContainsField(json, "name");
        });
    }

    [Test]
    public void DocumentTestHelpers_AssertJsonContainsField_ThrowsForMissingField()
    {
        // Arrange
        var json = "{\"id\": 1}";

        // Act & Assert
        Assert.Throws<NUnit.Framework.AssertionException>(() =>
        {
            DocumentTestHelpers.AssertJsonContainsField(json, "nonexistent");
        });
    }

    [Test]
    public void DocumentTestHelpers_GetJsonFieldValue_ReturnsCorrectValue()
    {
        // Arrange
        var json = "{\"id\": 42, \"name\": \"Test\"}";

        // Act
        var id = DocumentTestHelpers.GetJsonFieldValue<int>(json, "id");
        var name = DocumentTestHelpers.GetJsonFieldValue<string>(json, "name");

        // Assert
        Assert.That(id, Is.EqualTo(42));
        Assert.That(name, Is.EqualTo("Test"));
    }

    [Test]
    public void DocumentTestHelpers_AssertHtmlContainsEditForm_ValidatesCorrectly()
    {
        // Arrange
        var html = "<form><input type='text' /></form>";

        // Act & Assert
        Assert.DoesNotThrow(() =>
        {
            DocumentTestHelpers.AssertHtmlContainsEditForm(html);
        });
    }

    [Test]
    public void DocumentTestHelpers_AssertPathUsesCombine_ValidatesCorrectPaths()
    {
        // Arrange
        var validPath = Path.Combine("/home", "user", "file.txt");

        // Act & Assert
        Assert.DoesNotThrow(() =>
        {
            DocumentTestHelpers.AssertPathUsesCombine(validPath);
        });
    }

    [Test]
    public void DocumentTestHelpers_AssertTemplateFileIsValid_ValidatesFrxExtension()
    {
        // Arrange
        var templatePath = "/path/to/template.frx";

        // Act & Assert
        Assert.DoesNotThrow(() =>
        {
            DocumentTestHelpers.AssertTemplateFileIsValid(templatePath);
        });
    }

    [Test]
    public void DocumentTestHelpers_AssertConfigFileIsValid_ValidatesJsonExtension()
    {
        // Arrange
        var configPath = "/path/to/config.json";

        // Act & Assert
        Assert.DoesNotThrow(() =>
        {
            DocumentTestHelpers.AssertConfigFileIsValid(configPath);
        });
    }

    [Test]
    public void DocumentTestDataFixture_CreatePassportJson_ReturnsValidJson()
    {
        // Arrange & Act
        var json = DocumentTestDataFixture.CreatePassportJson(id: 1, idDevice: 1);

        // Assert
        Assert.That(json, Is.Not.Null);
        Assert.That(json, Is.Not.Empty);
        Assert.DoesNotThrow(() => Newtonsoft.Json.JsonConvert.DeserializeObject(json));

        // Проверка обязательных полей паспорта
        DocumentTestHelpers.AssertJsonContainsField(json, "id");
        DocumentTestHelpers.AssertJsonContainsField(json, "number");
        DocumentTestHelpers.AssertJsonContainsField(json, "productName");

        TestContext.WriteLine($"Passport JSON generated: {json.Length} characters");
    }

    [Test]
    public void DocumentTestDataFixture_CreateActJson_ReturnsValidJson()
    {
        // Arrange & Act
        var json = DocumentTestDataFixture.CreateActJson(id: 1, idDevice: 1);

        // Assert
        Assert.That(json, Is.Not.Null);
        Assert.DoesNotThrow(() => Newtonsoft.Json.JsonConvert.DeserializeObject(json));

        DocumentTestHelpers.AssertJsonContainsField(json, "id");
        DocumentTestHelpers.AssertJsonContainsField(json, "number");
        DocumentTestHelpers.AssertJsonContainsField(json, "shifts");
    }

    [Test]
    public void DocumentTestDataFixture_CreateKmhJson_ReturnsValidJson()
    {
        // Arrange & Act
        var json = DocumentTestDataFixture.CreateKmhJson(id: 1, idDevice: 1, "MPR_MPR");

        // Assert
        Assert.That(json, Is.Not.Null);
        Assert.DoesNotThrow(() => Newtonsoft.Json.JsonConvert.DeserializeObject(json));

        DocumentTestHelpers.AssertJsonContainsField(json, "measurementType");
        DocumentTestHelpers.AssertJsonContainsField(json, "measurements");
    }

    [Test]
    public void DocumentTestDataFixture_CreatePoverkaJson_ReturnsValidJson()
    {
        // Arrange & Act
        var json = DocumentTestDataFixture.CreatePoverkaJson(id: 1, idDevice: 1, "1974");

        // Assert
        Assert.That(json, Is.Not.Null);
        Assert.DoesNotThrow(() => Newtonsoft.Json.JsonConvert.DeserializeObject(json));

        DocumentTestHelpers.AssertJsonContainsField(json, "poverkaType");
        DocumentTestHelpers.AssertJsonContainsField(json, "measurements");
        DocumentTestHelpers.AssertJsonContainsField(json, "conclusion");
    }

    [Test]
    public void DocumentTestDataFixture_CreatePassportWithElisJson_IncludesElisData()
    {
        // Arrange & Act
        var json = DocumentTestDataFixture.CreatePassportWithElisJson(id: 1, idDevice: 1);

        // Assert
        Assert.That(json, Is.Not.Null);
        Assert.DoesNotThrow(() => Newtonsoft.Json.JsonConvert.DeserializeObject(json));

        DocumentTestHelpers.AssertJsonContainsField(json, "elisData");
        DocumentTestHelpers.AssertJsonContainsField(json, "elisData.labNumber");
        DocumentTestHelpers.AssertJsonContainsField(json, "elisData.parameters");

        TestContext.WriteLine("ELIS passport JSON contains laboratory data");
    }

    [Test]
    public void DocumentTestDataFixture_CreateMinimalEditFormHtml_ReturnsValidHtml()
    {
        // Arrange & Act
        var html = DocumentTestDataFixture.CreateMinimalEditFormHtml("Passport", 1);

        // Assert
        Assert.That(html, Is.Not.Null);
        Assert.That(html, Does.Contain("<form"));
        Assert.That(html, Does.Contain("<input"));
        Assert.That(html, Does.Contain("Passport"));

        TestContext.WriteLine($"HTML form generated: {html.Length} characters");
    }

    [Test]
    public void DocumentTestDataFixture_GetKmhMeasurementData_ReturnsCorrectData()
    {
        // Arrange & Act
        var mprData = DocumentTestDataFixture.GetKmhMeasurementData("MPR_MPR");
        var prData = DocumentTestDataFixture.GetKmhMeasurementData("PR_PR");
        var ppData = DocumentTestDataFixture.GetKmhMeasurementData("PP");

        // Assert
        Assert.That(mprData, Contains.Key("mass"));
        Assert.That(mprData, Contains.Key("density"));

        Assert.That(prData, Contains.Key("flow"));
        Assert.That(prData, Contains.Key("pressure"));

        Assert.That(ppData, Contains.Key("density"));
        Assert.That(ppData, Contains.Key("temperature"));

        TestContext.WriteLine("KMH measurement data fixtures are working correctly");
    }

    [Test]
    public void GetAllDocumentTypes_ReturnsNonEmptyArray()
    {
        // Arrange & Act
        var documentTypes = DocumentTestHelpers.GetAllDocumentTypes();

        // Assert
        Assert.That(documentTypes, Is.Not.Null);
        Assert.That(documentTypes.Length, Is.GreaterThan(0));

        TestContext.WriteLine($"Found {documentTypes.Length} document types for parameterized tests");
    }

    [Test]
    public void GetKmhDocumentTypes_ReturnsCorrectCount()
    {
        // Arrange & Act
        var kmhTypes = DocumentTestHelpers.GetKmhDocumentTypes();

        // Assert
        Assert.That(kmhTypes, Is.Not.Null);
        Assert.That(kmhTypes.Length, Is.EqualTo(10), "Should have 10 KMH document types");

        TestContext.WriteLine($"KMH document types: {kmhTypes.Length}");
    }

    [Test]
    public void GetPoverkaDocumentTypes_ReturnsCorrectCount()
    {
        // Arrange & Act
        var poverkaTypes = DocumentTestHelpers.GetPoverkaDocumentTypes();

        // Assert
        Assert.That(poverkaTypes, Is.Not.Null);
        Assert.That(poverkaTypes.Length, Is.EqualTo(8), "Should have 8 Poverka document types in helper");

        TestContext.WriteLine($"Poverka document types: {poverkaTypes.Length}");
    }
}
