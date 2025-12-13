using System.IO;
using System.Linq;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Tests.Configs;

extern alias PassportLib;

[TestFixture]
public class CfgEditPassportTests
{
    private string _repoRoot;

    [SetUp]
    public void SetUp()
    {
        // TestDirectory = Tests/bin/Debug/net8.0, нужно подняться на 4 уровня до корня репозитория
        _repoRoot = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", ".."));
    }

    [Test]
    public void Deserialize_WithIsBallast_PreservesClassification()
    {
        var configPath = Path.Combine(_repoRoot, "TN_Doc", "Cfg", "Passport", "CfgEditPassport_GOSTR50.2.040(I).json");
        Assert.That(File.Exists(configPath), $"Не найден файл конфигурации {configPath}");

        var json = File.ReadAllText(configPath);
        var cfg = JsonConvert.DeserializeObject<PassportLib::TN.Doc.Edit.CfgEditPassport>(json);

        Assert.That(cfg, Is.Not.Null);
        Assert.That(cfg.Parameters, Is.Not.Empty);

        var tempCorrection = cfg.Parameters.First(p => p.Key == "TempCorrection");
        var sulfurCorrection = cfg.Parameters.First(p => p.Key == "SulfurCorrection");

        Assert.Multiple(() =>
        {
            Assert.That(tempCorrection.IsBallast, Is.True, "TempCorrection должен быть балластным");
            Assert.That(sulfurCorrection.IsBallast, Is.False, "SulfurCorrection должен быть небалластным");
        });
    }

    [Test]
    public void Deserialize_WithoutIsBallast_DefaultsToFalse()
    {
        const string json = """
        {
          "Parameters": [
            {
              "Id": 1,
              "Key": "TempCorrection",
              "Name": "Температура нефти",
              "Use": true,
              "Edit": true
            }
          ]
        }
        """;

        var cfg = JsonConvert.DeserializeObject<PassportLib::TN.Doc.Edit.CfgEditPassport>(json);

        Assert.That(cfg, Is.Not.Null);
        Assert.That(cfg.Parameters, Has.Count.EqualTo(1));
        Assert.That(cfg.Parameters.Single().IsBallast, Is.False);
    }
}

