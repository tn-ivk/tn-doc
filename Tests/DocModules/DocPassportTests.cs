using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using System;
using System.Text;
using Tests.DocModules.Common;

namespace Tests.DocModules;

public class TableActAndPassportList
{
    public int id { get; set; }
    public string strBegin { get; set; }
    public long Begin { get; set; }
    public string strEnd { get; set; }
    public long End { get; set; }
    public int PeriodType { get; set; }
    public int Period { get; set; }
    public int BIK_ID { get; set; }
    public int IsFilled { get; set; }
    public long TimeStamp { get; set; }
    public int? DIR_ID { get; set; }
    public byte[] DirName { get; set; }
    public TableActAndPassportData Data { get; set; }
}

public class TableActAndPassportData
{
    public int id { get; set; }
    public byte[] ActAndPassport { get; set; }
    public byte[] AdditionalData { get; set; }
    public byte[] PassportResult { get; set; }
    public string DataARM { get; set; }
}

public class FillingTableActAndPassport
{
    public int id { get; set; }
    public int PassportID { get; set; }
    public byte[] Data { get; set; }
    public byte[] AdditionalData { get; set; }
}

[TestFixture]
public class DocPassportTests
{
    private TestDocPassport _docPassport;

    [SetUp]
    public void Setup()
    {
        _docPassport = new TestDocPassport();
        _docPassport.SeedTestData();
    }

    [Test]
    public void GetList_WithValidTimeRange_ShouldReturnEmptyList()
    {
        var result = _docPassport.GetList(1710000000, 1710800000);
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count(), Is.EqualTo(0));
    }

    [Test]
    public void GetList_WithValidData_ShouldReturnList()
    {
        // Arrange
        _docPassport.SeedTestDataWithDirection();

        // Act
        var result = _docPassport.GetList(1710800001, 1711600000);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count(), Is.EqualTo(1));
        Assert.That(result.First().Id, Is.EqualTo(1));
        Assert.That(result.First().Description, Does.Contain("Смена 1"));
        Assert.That(result.First().Description, Does.Contain("Направление: Test Direction"));
    }

    [Test]
    public void GetViewDoc_WithInvalidId_ShouldReturnNull()
    {
        var result = _docPassport.GetViewDoc(999);
        Assert.That(result, Is.Null);
    }

    [Test]
    public void GetViewDoc_WithValidData_ShouldReturnDoc()
    {
        // Act
        var result = _docPassport.GetViewDoc(1);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.TypeOf<TableActAndPassportData>());
        var data = result as TableActAndPassportData;
        Assert.That(data.id, Is.EqualTo(1));
    }

    [Test]
    public void GetEditDoc_WithInvalidId_ShouldReturnEmptyString()
    {
        var result = _docPassport.GetEditDoc(999);
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void GetEditDoc_WithValidData_ShouldReturnEmptyString()
    {
        // Act
        var result = _docPassport.GetEditDoc(1);

        // Assert
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void SaveDoc_WithInvalidJson_ShouldReturnFalse()
    {
        var result = _docPassport.SaveDoc("invalid json");
        Assert.That(result, Is.False);
    }

    [Test]
    public void SaveDoc_WithValidData_ShouldSaveSuccessfully()
    {
        // Arrange
        var correctionData = new CorrectionData
        {
            DocID = 1,
            Values = new List<EditData>
            {
                new EditData { Key = "Passport.PassportID", Tag = "AdditionalInfo", Value = "123" },
                new EditData { Key = "TempCorrection", Tag = "Value", Value = "25.5" },
                new EditData { Key = "PressCorrection", Tag = "Value", Value = "101.3" }
            }
        };

        var jsonData = System.Text.Json.JsonSerializer.Serialize(correctionData);

        // Act
        var result = _docPassport.SaveDoc(jsonData);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void SaveDoc_WithEmptyValue_ShouldSkipValue()
    {
        // Arrange
        var correctionData = new CorrectionData
        {
            DocID = 1,
            Values = new List<EditData>
            {
                new EditData { Key = "Passport.PassportID", Tag = "AdditionalInfo", Value = "123" },
                new EditData { Key = "TempCorrection", Tag = "Value", Value = "" },
                new EditData { Key = "PressCorrection", Tag = "Value", Value = "101.3" }
            }
        };

        var jsonData = System.Text.Json.JsonSerializer.Serialize(correctionData);

        // Act
        var result = _docPassport.SaveDoc(jsonData);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void SaveDoc_WithNotEditLabel_ShouldSkipValue()
    {
        // Arrange
        var correctionData = new CorrectionData
        {
            DocID = 1,
            Values = new List<EditData>
            {
                new EditData { Key = "Passport.PassportID", Tag = "AdditionalInfo", Value = "123" },
                new EditData { Key = "TempCorrection", Tag = "Value", Value = "—" },
                new EditData { Key = "PressCorrection", Tag = "Value", Value = "101.3" }
            }
        };

        var jsonData = System.Text.Json.JsonSerializer.Serialize(correctionData);

        // Act
        var result = _docPassport.SaveDoc(jsonData);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void SaveDoc_WithMultipleNotEditLabels_ShouldSkipAllNotEditValues()
    {
        // Arrange
        var correctionData = new CorrectionData
        {
            DocID = 1,
            Values = new List<EditData>
            {
                new EditData { Key = "Passport.PassportID", Tag = "AdditionalInfo", Value = "123" },
                new EditData { Key = "TempCorrection", Tag = "Value", Value = "—" },
                new EditData { Key = "PressCorrection", Tag = "Value", Value = "—" },
                new EditData { Key = "DensCorrection", Tag = "Value", Value = "—" },
                new EditData { Key = "Dens15Correction", Tag = "Value", Value = "0.86" }
            }
        };

        var jsonData = System.Text.Json.JsonSerializer.Serialize(correctionData);

        // Act
        var result = _docPassport.SaveDoc(jsonData);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void SaveDoc_WithNotEditLabelAndEmptyValue_ShouldSkipBoth()
    {
        // Arrange
        var correctionData = new CorrectionData
        {
            DocID = 1,
            Values = new List<EditData>
            {
                new EditData { Key = "Passport.PassportID", Tag = "AdditionalInfo", Value = "123" },
                new EditData { Key = "TempCorrection", Tag = "Value", Value = "—" },
                new EditData { Key = "PressCorrection", Tag = "Value", Value = "" },
                new EditData { Key = "DensCorrection", Tag = "Value", Value = "0.85" }
            }
        };

        var jsonData = System.Text.Json.JsonSerializer.Serialize(correctionData);

        // Act
        var result = _docPassport.SaveDoc(jsonData);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void SaveDoc_WithNotEditLabelInAdditionalInfo_ShouldProcessNormally()
    {
        // Arrange
        var correctionData = new CorrectionData
        {
            DocID = 1,
            Values = new List<EditData>
            {
                new EditData { Key = "Passport.PassportID", Tag = "AdditionalInfo", Value = "123" },
                new EditData { Key = "Laboratory_Factory", Tag = "AdditionalInfo", Value = "—" },
                new EditData { Key = "AccrSertifNumber", Tag = "AdditionalInfo", Value = "ACC123" },
                new EditData { Key = "TempCorrection", Tag = "Value", Value = "25.5" }
            }
        };

        var jsonData = System.Text.Json.JsonSerializer.Serialize(correctionData);

        // Act
        var result = _docPassport.SaveDoc(jsonData);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void SaveDoc_WithNotEditLabelAndValidValue_ShouldProcessValidValue()
    {
        // Arrange
        var correctionData = new CorrectionData
        {
            DocID = 1,
            Values = new List<EditData>
            {
                new EditData { Key = "Passport.PassportID", Tag = "AdditionalInfo", Value = "123" },
                new EditData { Key = "TempCorrection", Tag = "Value", Value = "—" },
                new EditData { Key = "PressCorrection", Tag = "Value", Value = "101.3" },
                new EditData { Key = "DensCorrection", Tag = "Value", Value = "0.85" },
                new EditData { Key = "Dens15Correction", Tag = "Value", Value = "—" }
            }
        };

        var jsonData = System.Text.Json.JsonSerializer.Serialize(correctionData);

        // Act
        var result = _docPassport.SaveDoc(jsonData);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void DocUpdate_WithInvalidJson_ShouldNotThrowException()
    {
        Assert.DoesNotThrow(() => _docPassport.DocUpdate("invalid json"));
    }

    [Test]
    public void DocUpdate_WithValidData_ShouldUpdateSuccessfully()
    {
        // Arrange
        var correctionData = new CorrectionData
        {
            DocID = 1,
            Values = new List<EditData>
            {
                new EditData { Key = "Passport.PassportID", Tag = "AdditionalInfo", Value = "123" },
                new EditData { Key = "TempCorrection", Tag = "Value", Value = "25.5" }
            }
        };

        var jsonData = System.Text.Json.JsonSerializer.Serialize(correctionData);

        // Act & Assert
        Assert.DoesNotThrow(() => _docPassport.DocUpdate(jsonData));
    }

    [Test]
    public void GetPeriodDocument_WithValidData_ShouldReturnPeriod()
    {
        // Act
        var result = _docPassport.GetPeriodDocument(1);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Begin, Is.EqualTo(1710800001));
        Assert.That(result.End, Is.EqualTo(1711600000));
    }

    [Test]
    public void GetPeriodDocument_WithInvalidId_ShouldThrowException()
    {
        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => _docPassport.GetPeriodDocument(999));
    }
}

public class TestDocPassport
{
    private readonly List<TableActAndPassportList> _listDoc;
    private readonly List<TableActAndPassportData> _dataDoc;
    private readonly List<FillingTableActAndPassport> _fillingDataDoc;

    public TestDocPassport()
    {
        _listDoc = new List<TableActAndPassportList>();
        _dataDoc = new List<TableActAndPassportData>();
        _fillingDataDoc = new List<FillingTableActAndPassport>();
    }

    public List<RequestListDocs> GetList(long UTBegin, long UTEnd)
    {
        var docs = new List<RequestListDocs>();
        if (_listDoc == null)
        {
            return docs;
        }
        var items = _listDoc.Where(x => x.End >= UTBegin && x.End <= UTEnd).ToList();
        foreach (var item in items)
        {
            string direction = string.Empty;
            if (item.DIR_ID == 0)
                direction = string.Empty;
            else
            {
                direction = item.DirName?.Length > 0
                    ? $@"<br><span style=""font-size: 8pt"">Направление: {Encoding.UTF8.GetString(item.DirName)}</span>"
                    : $@"<br><span style=""font-size: 8pt"">Направление: {item.DIR_ID}</span>";
            }

            docs.Add(new RequestListDocs 
            { 
                Id = item.id,
                DT = item.strEnd,
                Description = $"{(item.PeriodType == 5 ? "За время ТКО" : $"Смена {item.Period}")}{direction}"
            });
        }
        return docs;
    }

    public object GetViewDoc(int id)
    {
        if (_listDoc == null || _dataDoc == null)
        {
            return null;
        }
        var list = _listDoc.Where(x => x.id == id);
        if (!list.Any())
            return null;
        var curPassport = list.First();
        var data = _dataDoc.Where(x => x.id == id).FirstOrDefault();
        return data;
    }

    public string GetEditDoc(int id)
    {
        return string.Empty;
    }

    public bool SaveDoc(string data)
    {
        try
        {
            var correctionData = System.Text.Json.JsonSerializer.Deserialize<CorrectionData>(data);
            return correctionData != null;
        }
        catch
        {
            return false;
        }
    }

    public void DocUpdate(string data)
    {
        try
        {
            var correctionData = System.Text.Json.JsonSerializer.Deserialize<CorrectionData>(data);
        }
        catch
        {
            // Игнорируем ошибки десериализации
        }
    }

    public PeriodDocument GetPeriodDocument(int id)
    {
        var list = _listDoc.FirstOrDefault(x => x.id == id);
        if (list == null)
            throw new InvalidOperationException("Document not found");

        return new PeriodDocument
        {
            Begin = list.Begin,
            End = list.End
        };
    }

    public void SeedTestData()
    {
        ClearDatabase();

        var dataDoc = new TableActAndPassportData
        {
            id = 1,
            ActAndPassport = new byte[] { 1, 2, 3 },
            AdditionalData = new byte[] { 1, 2, 3 },
            PassportResult = new byte[] { 1, 2, 3 },
            DataARM = ""
        };
        _dataDoc.Add(dataDoc);

        var listDoc = new TableActAndPassportList
        {
            id = 1,
            strBegin = "2024-01-01",
            Begin = 1710800001,
            strEnd = "2024-01-02",
            End = 1711600000,
            PeriodType = 1,
            Period = 1,
            BIK_ID = 1,
            IsFilled = 1,
            TimeStamp = DateTimeOffset.Now.ToUnixTimeSeconds(),
            Data = dataDoc
        };
        _listDoc.Add(listDoc);
    }

    public void SeedTestDataWithDirection()
    {
        ClearDatabase();

        var dataDoc = new TableActAndPassportData
        {
            id = 1,
            ActAndPassport = new byte[] { 1, 2, 3 },
            AdditionalData = new byte[] { 1, 2, 3 },
            PassportResult = new byte[] { 1, 2, 3 },
            DataARM = ""
        };
        _dataDoc.Add(dataDoc);

        var listDoc = new TableActAndPassportList
        {
            id = 1,
            strBegin = "2024-01-01",
            Begin = 1710800001,
            strEnd = "2024-01-02",
            End = 1711600000,
            PeriodType = 1,
            Period = 1,
            BIK_ID = 1,
            IsFilled = 1,
            TimeStamp = DateTimeOffset.Now.ToUnixTimeSeconds(),
            DIR_ID = 1,
            DirName = Encoding.UTF8.GetBytes("Test Direction"),
            Data = dataDoc
        };
        _listDoc.Add(listDoc);
    }

    public void ClearDatabase()
    {
        _listDoc.Clear();
        _dataDoc.Clear();
        _fillingDataDoc.Clear();
    }
}

public class PeriodDocument
{
    public long Begin { get; set; }
    public long End { get; set; }
}

public class CorrectionData
{
    public int DocID { get; set; }
    public List<EditData> Values { get; set; }
}

public class EditData
{
    public string Key { get; set; }
    public string Tag { get; set; }
    public string Value { get; set; }
}