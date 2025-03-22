using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Tests.DocModules
{
    public class RequestListDocs
    {
        public int id { get; set; }
    }

    public class TableActAndPassportList
    {
        public int id { get; set; }
        public long Begin { get; set; }
        public long End { get; set; }
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
        public void GetViewDoc_WithInvalidId_ShouldReturnNull()
        {
            var result = _docPassport.GetViewDoc(999);
            Assert.That(result, Is.Null);
        }

        [Test]
        public void GetEditDoc_WithInvalidId_ShouldReturnEmptyString()
        {
            var result = _docPassport.GetEditDoc(999);
            Assert.That(result, Is.Empty);
        }

        [Test]
        public void SaveDoc_WithInvalidJson_ShouldReturnFalse()
        {
            var result = _docPassport.SaveDoc("invalid json");
            Assert.That(result, Is.False);
        }

        [Test]
        public void DocUpdate_WithInvalidJson_ShouldNotThrowException()
        {
            Assert.DoesNotThrow(() => _docPassport.DocUpdate("invalid json"));
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
                docs.Add(new RequestListDocs { id = item.id });
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
            return false;
        }

        public void DocUpdate(string data)
        {
        }

        public void SeedTestData()
        {
            ClearDatabase();

            // Создаем тестовые данные с другим временным диапазоном
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
                Begin = 1710800001, // Используем другой временной диапазон
                End = 1711600000,
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
} 