using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using TN.Doc;
using TN_DocGeneral.Services;
using TN.DocData;
using TN_DocGeneral.Dictionaries;
using Newtonsoft.Json;

namespace Tests.Services
{
    public class TestDocPassport : DocPassport
    {
        public TestDocPassport(DbContextOptions<DocGeneral> options, IAppConfigService appConfig, int idDevice, IdDoc idDoc, string path) 
            : base(options, appConfig, idDevice, idDoc, path)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Для тестов не нужно получать информацию о таблице из базы данных
            modelBuilder.Entity<TableActAndPassportList>()
                .HasKey(e => e.id);

            modelBuilder.Entity<TableActAndPassportData>()
                .HasKey(e => e.id);

            modelBuilder.Entity<FillingTableActAndPassport>()
                .HasKey(e => e.id);

            modelBuilder.Entity<TableActAndPassportList>()
                .HasOne(e => e.Data)
                .WithOne()
                .HasForeignKey<TableActAndPassportData>(e => e.id);
        }

        public void ClearDatabase()
        {
            ListDoc.RemoveRange(ListDoc);
            DataDoc.RemoveRange(DataDoc);
            SaveChanges();
        }

        public void SeedTestData()
        {
            ClearDatabase();

            var testData = new TableActAndPassportData
            {
                id = 1,
                ActAndPassport = new byte[] { },
                AdditionalData = new byte[] { },
                PassportResult = new byte[] { },
                DataARM = ""
            };
            DataDoc.Add(testData);

            var testList = new TableActAndPassportList
            {
                id = 1,
                strBegin = "2024-03-20 00:00:00",
                Begin = 1710892800,
                strEnd = "2024-03-20 23:59:59",
                End = 1710979199,
                PeriodType = 1,
                Period = 1,
                BIK_ID = 1,
                IsFilled = 1,
                TimeStamp = 1710979199,
                DIR_ID = 1,
                Data = testData
            };
            ListDoc.Add(testList);

            SaveChanges();
        }
    }

    [TestFixture]
    public class DocPassportTests
    {
        private DbContextOptions<DocGeneral> _options;
        private Mock<IAppConfigService> _appConfigMock;
        private TestDocPassport _docPassport;
        private const int TEST_DEVICE_ID = 1;
        private const IdDoc TEST_DOC_ID = IdDoc.Passport;
        private const string TEST_PATH = "TestPath";

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            _options = new DbContextOptionsBuilder<DocGeneral>()
                .UseInMemoryDatabase(databaseName: "TestDb")
                .Options;
        }

        [SetUp]
        public void Setup()
        {
            _appConfigMock = new Mock<IAppConfigService>();

            // Настройка конфигурации
            var root = new Root
            {
                Doc = new TN.DocData.Doc
                {
                    Settings = new Settings
                    {
                        Dictionarys = new Dictionarys()
                    }
                }
            };

            // Настройка строк подключения
            var dbConnectionStrings = new List<DBConnectionString>
            {
                new DBConnectionString
                {
                    Use = true,
                    GuidDevice = TEST_DEVICE_ID,
                    Server = "localhost",
                    Userid = "test_user",
                    Password = "test_password",
                    Database = "test_db",
                    ConnectionTimeout = 1
                }
            };

            // Настройка конфигурации устройства
            var device = new Device
            {
                Use = true,
                IdDevice = TEST_DEVICE_ID,
                DBConnectionStrings = dbConnectionStrings,
                Docs = new List<Document>
                {
                    new Document
                    {
                        Use = true,
                        IdDoc = TEST_DOC_ID,
                        PathToDocConfigFile = "config.json",
                        PathToDocEditConfigFile = "edit_config.json",
                        TemplateDocs = new List<TemplateDoc>
                        {
                            new TemplateDoc
                            {
                                Use = true,
                                Id = 0,
                                PathToDocTemplateFile = "template.json"
                            }
                        }
                    }
                }
            };

            // Настройка мока
            _appConfigMock.Setup(x => x.GetCfg()).Returns(root);
            _appConfigMock.Setup(x => x.GetAppCfg()).Returns(new CfgApp());
            _appConfigMock.Setup(x => x.GetDeviceCfg(TEST_DEVICE_ID)).Returns(device);
            _appConfigMock.Setup(x => x.GetDocCfg(TEST_DEVICE_ID, TEST_DOC_ID)).Returns(device.Docs.First());
            _appConfigMock.Setup(x => x.GetPathConfigFile(TEST_DEVICE_ID, TEST_DOC_ID)).Returns("config.json");
            _appConfigMock.Setup(x => x.GetPathEditConfigFile(TEST_DEVICE_ID, TEST_DOC_ID)).Returns("edit_config.json");
            _appConfigMock.Setup(x => x.GetPathTemplateFile(TEST_DEVICE_ID, TEST_DOC_ID)).Returns("template.json");

            // Создание экземпляра тестируемого класса
            _docPassport = new TestDocPassport(_options, _appConfigMock.Object, TEST_DEVICE_ID, TEST_DOC_ID, TEST_PATH);
            _docPassport.SeedTestData();
        }

        [Test]
        public void Constructor_WithValidParameters_ShouldInitializeCorrectly()
        {
            Assert.That(_docPassport, Is.Not.Null);
            Assert.That(_docPassport.IdDoc, Is.EqualTo(TEST_DOC_ID));
        }

        [Test]
        public void GetList_WithValidTimeRange_ShouldReturnEmptyList()
        {
            var result = _docPassport.GetList(1710000000, 1710800000);
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(0));
        }

        [Test]
        public void GetViewDoc_WithInvalidId_ShouldReturnNull()
        {
            var result = _docPassport.GetViewDoc(-1);
            Assert.That(result, Is.Null);
        }

        [Test]
        public void GetEditDoc_WithInvalidId_ShouldReturnEmptyString()
        {
            var result = _docPassport.GetEditDoc(-1);
            Assert.That(result, Is.Empty);
        }

        [Test]
        public void SaveDoc_WithInvalidJson_ShouldReturnFalse()
        {
            // Arrange
            var invalidData = new TN.Doc.CorrectionData
            {
                DocID = -1,
                Values = new List<TN.Doc.EditData>
                {
                    new TN.Doc.EditData
                    {
                        Key = "invalid_key",
                        Tag = "invalid_tag",
                        Value = "invalid_value"
                    }
                }
            };
            var invalidJson = JsonConvert.SerializeObject(invalidData);

            // Act
            var result = _docPassport.SaveDoc(invalidJson);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void DocUpdate_WithInvalidJson_ShouldNotThrowException()
        {
            // Arrange
            var invalidData = new TN.Doc.CorrectionData
            {
                DocID = -1,
                Values = new List<TN.Doc.EditData>
                {
                    new TN.Doc.EditData
                    {
                        Key = "invalid_key",
                        Tag = "invalid_tag",
                        Value = "invalid_value"
                    }
                }
            };
            var invalidJson = JsonConvert.SerializeObject(invalidData);

            // Act & Assert
            Assert.DoesNotThrow(() => _docPassport.DocUpdate(invalidJson));
        }
    }
} 