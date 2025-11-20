extern alias PassportLib;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Moq;
using Newtonsoft.Json;
using NLog;
using NLog.Config;
using NLog.Targets;
using NUnit.Framework;
using TN_DocGeneral.Dictionaries;
using TN_DocGeneral.Services;
using TN.Utils.Helpers;
using PassportCore = PassportLib::TN.Doc;
using PassportEdit = PassportLib::TN.Doc.Edit;
using TN.DocData;
using DataSource = PassportLib::TN.Doc.DataSource;
using FieldHistoryEntry = PassportLib::TN.Doc.FieldHistoryEntry;
using DocGeneralBase = TN.Doc.DocGeneral;

namespace Tests.Services.Passport;

[TestFixture]
public class DocUpdateTests
{
	private const int DeviceId = 9001;
	private MemoryTarget _passportQualityTarget = null!;

	[TearDown]
	public void CleanupLogger()
	{
		LogManager.Configuration = null;
		_passportQualityTarget = null;
	}

	[Test]
	public void DocUpdate_ShouldAppendManualOverrideHistory_ForNonBallastParameter()
	{
		// Arrange
		var history = new Dictionary<string, List<FieldHistoryEntry>>
		{
			["value.SulfurCorrection"] = new List<FieldHistoryEntry>
			{
				CreateHistory(DataSource.ELIS, "0,40", DateTime.UtcNow.AddMinutes(-30))
			},
			["result.SulfurCorrection"] = new List<FieldHistoryEntry>
			{
				CreateHistory(DataSource.ELIS, "0,40", DateTime.UtcNow.AddMinutes(-30))
			}
		};

		var payload = new Dictionary<string, object>
		{
			["value.SulfurCorrection"] = "0,40",
			["value.SulfurCorrection__elisFilled"] = true,
			["result.SulfurCorrection"] = "менее 0,5",
			["result.SulfurCorrection__elisFilled"] = false,
			["method.SulfurCorrection"] = JsonConvert.SerializeObject(new PassportEdit.Metod { Id = 10, Name = "ASTM D4294" }),
			["method.SulfurCorrection__elisFilled"] = true,
			["__history"] = JsonConvert.SerializeObject(history)
		};

		using var passport = CreateDocPassport(isElisUsed: true);
		_passportQualityTarget = ConfigurePassportQualityTarget();

		// Act
		passport.DocUpdate(51234, payload);

		// Assert
		Assert.That(passport.LastDataArm, Is.Not.Null);

		var resultHistory = passport.LastDataArm!.FieldHistoryMap["result.SulfurCorrection"];
		Assert.That(resultHistory.Count, Is.EqualTo(2));
		Assert.That(resultHistory[^1].Source, Is.EqualTo(DataSource.Manual));
		Assert.That(resultHistory[^1].Comment, Does.Contain("Manual overrides ELIS"));

		var labInfo = passport.LastDataArm.LabInfo.Single(x => x.ParameterKey == "SulfurCorrection");
		Assert.That(labInfo.Value, Is.EqualTo("менее 0,5"));

		Assert.That(string.Join(Environment.NewLine, _passportQualityTarget!.Logs), Does.Contain("Manual overrides ELIS"));
	}

	[Test]
	public void DocUpdate_ShouldLogWarningForBallastMismatch()
	{
		// Arrange
		var history = new Dictionary<string, List<FieldHistoryEntry>>
		{
			["value.TempCorrection"] = new List<FieldHistoryEntry>
			{
				CreateHistory(DataSource.ELIS, "0,45", DateTime.UtcNow.AddMinutes(-20))
			},
			["result.TempCorrection"] = new List<FieldHistoryEntry>
			{
				CreateHistory(DataSource.ELIS, "0,45", DateTime.UtcNow.AddMinutes(-20))
			}
		};

		var payload = new Dictionary<string, object>
		{
			["value.TempCorrection"] = "0,45",
			["value.TempCorrection__elisFilled"] = true,
			["result.TempCorrection"] = "0,40",
			["result.TempCorrection__elisFilled"] = true,
			["__history"] = JsonConvert.SerializeObject(history)
		};

		using var passport = CreateDocPassport(isElisUsed: true);
		_passportQualityTarget = ConfigurePassportQualityTarget();

		// Act
		passport.DocUpdate(60001, payload);

		// Assert
		Assert.That(passport.LastDataArm, Is.Not.Null);
		var resultHistory = passport.LastDataArm!.FieldHistoryMap["result.TempCorrection"];
		Assert.That(resultHistory.Count, Is.EqualTo(1), "История должна остаться без добавленных записей");

		Assert.That(string.Join(Environment.NewLine, _passportQualityTarget!.Logs), Does.Contain("Балластный параметр"));
	}

	[Test]
	public void DocUpdate_ShouldPersistManualMethodAndHistory()
	{
		// Arrange
		var methodHistory = new Dictionary<string, List<FieldHistoryEntry>>
		{
			["method.TempCorrection"] = new List<FieldHistoryEntry>
			{
				CreateHistory(DataSource.Manual, "Ручной метод 05.2024", DateTime.UtcNow.AddMinutes(-5))
			}
		};

		var payload = new Dictionary<string, object>
		{
			["method.TempCorrection"] = JsonConvert.SerializeObject(new PassportEdit.Metod
			{
				Id = 0,
				Name = "Ручной метод 05.2024",
				LimitValueActivate = false
			}),
			["method.TempCorrection__elisFilled"] = false,
			["__history"] = JsonConvert.SerializeObject(methodHistory)
		};

		using var passport = CreateDocPassport(isElisUsed: true);

		// Act
		passport.DocUpdate(77777, payload);

		// Assert
		Assert.That(passport.LastDataArm, Is.Not.Null);

		var labInfo = passport.LastDataArm!.LabInfo.Single(x => x.ParameterKey == "TempCorrection");
		Assert.That(labInfo.Metod.Name, Is.EqualTo("Ручной метод 05.2024"));
		Assert.That(labInfo.ElisFilled, Is.False);

		var storedHistory = passport.LastDataArm.FieldHistoryMap["method.TempCorrection"];
		Assert.That(storedHistory.Count, Is.EqualTo(1));
		Assert.That(storedHistory[0].Source, Is.EqualTo(DataSource.Manual));
	}

	private static FieldHistoryEntry CreateHistory(DataSource source, string value, DateTime timestamp)
	{
		return new FieldHistoryEntry
		{
			Source = source,
			Value = value,
			ModifiedAt = timestamp,
			ModifiedBy = source == DataSource.ELIS ? "ELIS" : "Пользователь"
		};
	}

	private static MemoryTarget ConfigurePassportQualityTarget()
	{
		var config = new LoggingConfiguration();
		var target = new MemoryTarget { Layout = "${level}|${message}" };
		config.AddRule(LogLevel.Info, LogLevel.Fatal, target, "PassportQuality");
		LogManager.Configuration = config;
		return target;
	}

	private static string FindRepoRoot()
	{
		var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
		while (directory != null && !File.Exists(Path.Combine(directory.FullName, "TN_Doc.sln")))
		{
			directory = directory.Parent;
		}

		if (directory == null)
			throw new InvalidOperationException("Не удалось найти корень репозитория (TN_Doc.sln).");

		return directory.FullName;
	}

	private static Root CreateMinimalRoot()
	{
		return new Root
		{
			Doc = new Doc
			{
				Settings = new Settings
				{
					General = new General(),
					Header = new Header(),
					Data = new Data(),
					Footer = new Footer(),
					Dictionarys = new Dictionarys
					{
						Users = new List<Users>(),
						UsersGroup = new List<UsersGroup>(),
						Licenses = new List<License>()
					}
				},
				DataIVK = new DataIVK(),
				DataArm = new DataArm()
			}
		};
	}

	private static TestDocPassport CreateDocPassport(bool isElisUsed)
	{
		var rootPath = FindRepoRoot();
		var options = new DbContextOptionsBuilder<DocGeneralBase>()
			.UseInMemoryDatabase(Guid.NewGuid().ToString())
			.Options;

		var appConfig = new Mock<IAppConfigService>();
		var deviceCfg = new Device
		{
			IdDevice = DeviceId,
			Name = "DeviceUnderTest",
			DBConnectionStrings = new List<DBConnectionString>
			{
				new()
				{
					Use = true,
					Server = "localhost",
					Userid = "user",
					Password = "pwd",
					Database = "db",
					ConnectionTimeout = 30
				}
			},
			Docs = new List<Document>()
		};

		appConfig.Setup(x => x.GetCfg()).Returns(CreateMinimalRoot());
		appConfig.Setup(x => x.GetDeviceCfg(It.IsAny<int>())).Returns(deviceCfg);
		appConfig.Setup(x => x.GetPathConfigFile(It.IsAny<int>(), It.IsAny<IdDoc>()))
			.Returns("TN_Doc/Cfg/Passport/CfgEditPassport_GOSTR50.2.040(I).json");
		appConfig.Setup(x => x.GetPathEditConfigFile(It.IsAny<int>(), It.IsAny<IdDoc>()))
			.Returns("TN_Doc/Cfg/Passport/CfgEditPassport_GOSTR50.2.040(I).json");
		appConfig.Setup(x => x.GetPathTemplateFile(It.IsAny<int>(), It.IsAny<IdDoc>()))
			.Returns("TN_Doc/Doc/Passport.frx");
		appConfig.Setup(x => x.IsUsedElis(It.IsAny<int>())).Returns(() => isElisUsed);

		var cache = new Mock<IConfigurationCacheService>();
		cache.Setup(x => x.GetOrLoadConfig<PassportEdit.CfgEditPassport>(It.IsAny<string>()))
			.Returns((string path) => CfgFileRW.LoadCfg<PassportEdit.CfgEditPassport>(path));

		return new TestDocPassport(options, appConfig.Object, cache.Object, DeviceId, rootPath);
	}

	private sealed class TestDocPassport : PassportCore.DocPassport
	{
		public TestDocPassport(DbContextOptions<DocGeneralBase> options, IAppConfigService appConfig, IConfigurationCacheService configCache, int idDevice, string rootPath)
			: base(options, appConfig, configCache, idDevice, IdDoc.Passport, rootPath)
		{
		}

		public PassportCore.DataARM LastDataArm { get; private set; } = null!;

		protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
		{
			// Отключаем конфигурацию MySQL из базового класса для тестов
		}

		protected override void PersistDataArm(int documentId, string serializedDataArm)
		{
			LastDataArm = JsonDeserializeObject<PassportCore.DataARM>(serializedDataArm);
		}
	}
}

