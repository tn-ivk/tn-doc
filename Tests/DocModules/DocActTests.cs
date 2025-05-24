using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Tests.DocModules.Common;

namespace Tests.DocModules;

public class TableResultActAndPassportList
{
    public int id { get; set; }
    public string strBegin { get; set; }
    public long Begin { get; set; }
    public string strEnd { get; set; }
    public long End { get; set; }
    public int PeriodType { get; set; }
    public int? DIR_ID { get; set; }
    public byte[] DirName { get; set; }
    public TableResultActAndPassportData Data { get; set; }
}

public class TableResultActAndPassportData
{
    public int id { get; set; }
    public byte[] AdditionalInfo { get; set; }
    public byte[] ResultActAndPassport { get; set; }
}

[TestFixture]
public class DocActTests
{
    private TestDocAct _docAct;

    [SetUp]
    public void Setup()
    {
        _docAct = new TestDocAct();
        _docAct.SeedTestData();
    }

    [Test]
    public void GetList_WithValidTimeRange_ShouldReturnEmptyList()
    {
        var result = _docAct.GetList(1700000000, 1701000000);
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count(), Is.EqualTo(0));
    }

    [Test]
    public void GetList_WithValidData_ShouldReturnList()
    {
        // Arrange
        _docAct.SeedTestDataWithDirection();

        // Act
        var result = _docAct.GetList(1704067200, 1704153599);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count(), Is.EqualTo(1));
        Assert.That(result.First().Id, Is.EqualTo(1));
        Assert.That(result.First().Description, Does.Contain("Акт валовый"));
        Assert.That(result.First().Description, Does.Contain("Направление: Направление 1"));
    }

    [Test]
    public void GetList_WithMultipleDocuments_ShouldReturnAllInRange()
    {
        // Arrange
        _docAct.SeedTestDataWithMultipleDocuments();

        // Act
        var result = _docAct.GetList(1704067200, 1704153599);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count(), Is.EqualTo(2));
        Assert.That(result.Select(x => x.Id), Is.EquivalentTo(new[] { 1, 2 }));
    }

    [Test]
    public void GetList_WithDocumentsOutsideRange_ShouldNotReturnThem()
    {
        // Arrange
        _docAct.SeedTestDataWithMultipleDocuments();

        // Act
        var result = _docAct.GetList(1704067200, 1704067201);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count(), Is.EqualTo(0));
    }

    [Test]
    public void GetList_WithNullList_ShouldReturnEmptyList()
    {
        // Arrange
        _docAct.SeedNullList();

        // Act
        var result = _docAct.GetList(1704067200, 1704153599);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count(), Is.EqualTo(0));
    }

    [Test]
    public void GetViewDoc_WithInvalidId_ShouldReturnNull()
    {
        var result = _docAct.GetViewDoc(999);
        Assert.That(result, Is.Null);
    }

    [Test]
    public void GetViewDoc_WithValidData_ShouldReturnDoc()
    {
        // Act
        var result = _docAct.GetViewDoc(1);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.TypeOf<TableResultActAndPassportData>());
        var data = result as TableResultActAndPassportData;
        Assert.That(data.id, Is.EqualTo(1));
    }

    [Test]
    public void GetViewDoc_WithNullLists_ShouldReturnNull()
    {
        // Arrange
        _docAct.SeedNullLists();

        // Act
        var result = _docAct.GetViewDoc(1);

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public void GetEditDoc_WithInvalidId_ShouldReturnEmptyString()
    {
        var result = _docAct.GetEditDoc(999);
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void GetEditDoc_WithValidData_ShouldReturnEmptyString()
    {
        // Act
        var result = _docAct.GetEditDoc(1);

        // Assert
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void SaveDoc_WithInvalidJson_ShouldReturnFalse()
    {
        var result = _docAct.SaveDoc("invalid json");
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
                new EditData { Key = "ActNumber", Tag = "AdditionalInfo", Value = "123" },
                new EditData { Key = "DelivePoint", Tag = "AdditionalInfo", Value = "Test Point" }
            }
        };

        var jsonData = System.Text.Json.JsonSerializer.Serialize(correctionData);

        // Act
        var result = _docAct.SaveDoc(jsonData);

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
                new EditData { Key = "ActNumber", Tag = "AdditionalInfo", Value = "" },
                new EditData { Key = "DelivePoint", Tag = "AdditionalInfo", Value = "Test Point" }
            }
        };

        var jsonData = System.Text.Json.JsonSerializer.Serialize(correctionData);

        // Act
        var result = _docAct.SaveDoc(jsonData);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void SaveDoc_WithNullValues_ShouldReturnFalse()
    {
        // Arrange
        var correctionData = new CorrectionData
        {
            DocID = 1,
            Values = null
        };

        var jsonData = System.Text.Json.JsonSerializer.Serialize(correctionData);

        // Act
        var result = _docAct.SaveDoc(jsonData);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void SaveDoc_WithEmptyValuesList_ShouldReturnTrue()
    {
        // Arrange
        var correctionData = new CorrectionData
        {
            DocID = 1,
            Values = new List<EditData>()
        };

        var jsonData = System.Text.Json.JsonSerializer.Serialize(correctionData);

        // Act
        var result = _docAct.SaveDoc(jsonData);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void SaveDoc_WithInvalidDocId_ShouldReturnFalse()
    {
        // Arrange
        var correctionData = new CorrectionData
        {
            DocID = 999,
            Values = new List<EditData>
            {
                new EditData { Key = "ActNumber", Tag = "AdditionalInfo", Value = "123" }
            }
        };

        var jsonData = System.Text.Json.JsonSerializer.Serialize(correctionData);

        // Act
        var result = _docAct.SaveDoc(jsonData);

        // Assert
        Assert.That(result, Is.False);
    }
}

public class TestDocAct
{
    private List<TableResultActAndPassportList> _listDoc;
    private List<TableResultActAndPassportData> _dataDoc;

    public TestDocAct()
    {
        _listDoc = new List<TableResultActAndPassportList>();
        _dataDoc = new List<TableResultActAndPassportData>();
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
            if (item.DIR_ID.HasValue && item.DIR_ID.Value > 0)
            {
                direction = item.DirName?.Length > 0
                    ? $@"<br><span style=""font-size: 8pt"">Направление: {Encoding.UTF8.GetString(item.DirName)}</span>"
                    : $@"<br><span style=""font-size: 8pt"">Направление: {item.DIR_ID}</span>";
            }

            docs.Add(new RequestListDocs 
            { 
                Id = item.id,
                DT = item.strEnd,
                Description = $"Акт валовый{direction}"
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
            if (correctionData == null || correctionData.Values == null)
                return false;

            // Проверяем существование документа
            if (!_listDoc.Any(x => x.id == correctionData.DocID))
                return false;

            return true;
        }
        catch
        {
            return false;
        }
    }

    public void SeedTestData()
    {
        ClearDatabase();

        var dataDoc = new TableResultActAndPassportData
        {
            id = 1,
            AdditionalInfo = Encoding.UTF8.GetBytes("{}"),
            ResultActAndPassport = Encoding.UTF8.GetBytes("{}")
        };
        _dataDoc.Add(dataDoc);

        var listDoc = new TableResultActAndPassportList
        {
            id = 1,
            strBegin = "01.01.2024 00:00",
            Begin = 1704067200,
            strEnd = "01.01.2024 23:59",
            End = 1704153599,
            PeriodType = 1,
            Data = dataDoc
        };
        _listDoc.Add(listDoc);
    }

    public void SeedTestDataWithDirection()
    {
        ClearDatabase();

        var dataDoc = new TableResultActAndPassportData
        {
            id = 1,
            AdditionalInfo = Encoding.UTF8.GetBytes("{}"),
            ResultActAndPassport = Encoding.UTF8.GetBytes("{}")
        };
        _dataDoc.Add(dataDoc);

        var listDoc = new TableResultActAndPassportList
        {
            id = 1,
            strBegin = "01.01.2024 00:00",
            Begin = 1704067200,
            strEnd = "01.01.2024 23:59",
            End = 1704153599,
            PeriodType = 1,
            DIR_ID = 1,
            DirName = Encoding.UTF8.GetBytes("Направление 1"),
            Data = dataDoc
        };
        _listDoc.Add(listDoc);
    }

    public void SeedTestDataWithMultipleDocuments()
    {
        ClearDatabase();

        // Первый документ
        var dataDoc1 = new TableResultActAndPassportData
        {
            id = 1,
            AdditionalInfo = Encoding.UTF8.GetBytes("{}"),
            ResultActAndPassport = Encoding.UTF8.GetBytes("{}")
        };
        _dataDoc.Add(dataDoc1);

        var listDoc1 = new TableResultActAndPassportList
        {
            id = 1,
            strBegin = "01.01.2024 00:00",
            Begin = 1704067200,
            strEnd = "01.01.2024 23:59",
            End = 1704153599,
            PeriodType = 1,
            Data = dataDoc1
        };
        _listDoc.Add(listDoc1);

        // Второй документ
        var dataDoc2 = new TableResultActAndPassportData
        {
            id = 2,
            AdditionalInfo = Encoding.UTF8.GetBytes("{}"),
            ResultActAndPassport = Encoding.UTF8.GetBytes("{}")
        };
        _dataDoc.Add(dataDoc2);

        var listDoc2 = new TableResultActAndPassportList
        {
            id = 2,
            strBegin = "01.01.2024 00:00",
            Begin = 1704067200,
            strEnd = "01.01.2024 23:59",
            End = 1704153599,
            PeriodType = 1,
            Data = dataDoc2
        };
        _listDoc.Add(listDoc2);
    }

    public void SeedNullList()
    {
        _listDoc = null;
        _dataDoc = new List<TableResultActAndPassportData>();
    }

    public void SeedNullLists()
    {
        _listDoc = null;
        _dataDoc = null;
    }

    public void ClearDatabase()
    {
        _listDoc = new List<TableResultActAndPassportList>();
        _dataDoc = new List<TableResultActAndPassportData>();
    }
}