using System.Collections.Generic;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Tests.Configs;

extern alias PassportLib;

using QualityParameterSchema = PassportLib::TN.DocEditor.Passport.QualityParameterSchema;
using Parameter = PassportLib::TN.Doc.Edit.Parameter;
using CfgEditPassport = PassportLib::TN.Doc.Edit.CfgEditPassport;

/// <summary>
/// Тесты для механизма LinkedParameters (объединение методов испытаний между связанными параметрами)
/// </summary>
[TestFixture]
public class CfgEditPassportLinkedParametersTests
{
    [Test]
    public void Deserialize_WithLinkedParameter_PreservesLink()
    {
        const string json = """
        {
          "Parameters": [
            {
              "Id": 7,
              "Key": "Chloride_Salts.Concentration",
              "Name": "Массовая концентрация хлористых солей, мг/дм³",
              "LinkedParameter": "Chloride_Salts.MassFraction",
              "Use": true,
              "Edit": true
            },
            {
              "Id": 8,
              "Key": "Chloride_Salts.MassFraction",
              "Name": "Массовая доля хлористых солей, %",
              "Use": true,
              "Edit": true
            }
          ]
        }
        """;

        var cfg = JsonConvert.DeserializeObject<CfgEditPassport>(json);

        Assert.That(cfg, Is.Not.Null);
        Assert.That(cfg.Parameters, Has.Count.EqualTo(2));
        Assert.That(cfg.Parameters[0].LinkedParameter, Is.EqualTo("Chloride_Salts.MassFraction"));
        Assert.That(cfg.Parameters[1].LinkedParameter, Is.Null);
    }

    [Test]
    public void Deserialize_WithoutLinkedParameter_DefaultsToNull()
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

        var cfg = JsonConvert.DeserializeObject<CfgEditPassport>(json);

        Assert.That(cfg, Is.Not.Null);
        Assert.That(cfg.Parameters, Has.Count.EqualTo(1));
        Assert.That(cfg.Parameters[0].LinkedParameter, Is.Null);
    }

    [Test]
    public void ResolveLinkedParametersRoles_SetsLeaderAndFollower()
    {
        // Arrange
        var schemas = new List<QualityParameterSchema>
        {
            new() { Key = "Chloride_Salts.Concentration" },
            new() { Key = "Chloride_Salts.MassFraction" }
        };

        var cfg = new CfgEditPassport
        {
            Parameters = new List<Parameter>
            {
                new()
                {
                    Key = "Chloride_Salts.Concentration",
                    LinkedParameter = "Chloride_Salts.MassFraction",
                    Use = true
                },
                new()
                {
                    Key = "Chloride_Salts.MassFraction",
                    Use = true
                }
            }
        };

        // Act
        ResolveLinkedParametersRoles(schemas, cfg);

        // Assert - Ведущий параметр
        var leader = schemas.Find(s => s.Key == "Chloride_Salts.Concentration");
        Assert.That(leader.LinkedParameter, Is.EqualTo("Chloride_Salts.MassFraction"));
        Assert.That(leader.IsLinkedFollower, Is.Null.Or.False);

        // Assert - Ведомый параметр
        var follower = schemas.Find(s => s.Key == "Chloride_Salts.MassFraction");
        Assert.That(follower.IsLinkedFollower, Is.True);
        Assert.That(follower.LinkedLeaderKey, Is.EqualTo("Chloride_Salts.Concentration"));
    }

    [Test]
    public void ResolveLinkedParametersRoles_SlaveKeyHasPriority()
    {
        // Arrange - параметр имеет и SlaveKey, и LinkedParameter
        var schemas = new List<QualityParameterSchema>
        {
            new() { Key = "DNP.kPa" },
            new() { Key = "DNP.mercury_mm" }
        };

        var cfg = new CfgEditPassport
        {
            Parameters = new List<Parameter>
            {
                new()
                {
                    Key = "DNP.kPa",
                    SlaveKey = "DNP.mercury_mm",
                    LinkedParameter = "DNP.mercury_mm", // Должен быть проигнорирован
                    Use = true
                },
                new()
                {
                    Key = "DNP.mercury_mm",
                    Use = true
                }
            }
        };

        // Act
        ResolveLinkedParametersRoles(schemas, cfg);

        // Assert - LinkedParameter игнорируется, так как SlaveKey имеет приоритет
        var leader = schemas.Find(s => s.Key == "DNP.kPa");
        Assert.That(leader.LinkedParameter, Is.Null);

        var follower = schemas.Find(s => s.Key == "DNP.mercury_mm");
        Assert.That(follower.IsLinkedFollower, Is.Null.Or.False);
        Assert.That(follower.LinkedLeaderKey, Is.Null);
    }

    [Test]
    public void ResolveLinkedParametersRoles_IgnoresDisabledParameters()
    {
        // Arrange - параметр Use = false
        var schemas = new List<QualityParameterSchema>
        {
            new() { Key = "Chloride_Salts.Concentration" },
            new() { Key = "Chloride_Salts.MassFraction" }
        };

        var cfg = new CfgEditPassport
        {
            Parameters = new List<Parameter>
            {
                new()
                {
                    Key = "Chloride_Salts.Concentration",
                    LinkedParameter = "Chloride_Salts.MassFraction",
                    Use = false // Отключён
                },
                new()
                {
                    Key = "Chloride_Salts.MassFraction",
                    Use = true
                }
            }
        };

        // Act
        ResolveLinkedParametersRoles(schemas, cfg);

        // Assert - никакие связи не установлены
        var leader = schemas.Find(s => s.Key == "Chloride_Salts.Concentration");
        Assert.That(leader.LinkedParameter, Is.Null);

        var follower = schemas.Find(s => s.Key == "Chloride_Salts.MassFraction");
        Assert.That(follower.IsLinkedFollower, Is.Null.Or.False);
    }

    [Test]
    public void ResolveLinkedParametersRoles_HandlesNonExistentFollower()
    {
        // Arrange - LinkedParameter указывает на несуществующий параметр
        var schemas = new List<QualityParameterSchema>
        {
            new() { Key = "Chloride_Salts.Concentration" }
            // Chloride_Salts.MassFraction отсутствует в схеме
        };

        var cfg = new CfgEditPassport
        {
            Parameters = new List<Parameter>
            {
                new()
                {
                    Key = "Chloride_Salts.Concentration",
                    LinkedParameter = "Chloride_Salts.MassFraction",
                    Use = true
                }
            }
        };

        // Act - не должно выбросить исключение
        Assert.DoesNotThrow(() => ResolveLinkedParametersRoles(schemas, cfg));

        // Assert - ведущий параметр получил LinkedParameter, но ведомый не найден
        var leader = schemas.Find(s => s.Key == "Chloride_Salts.Concentration");
        Assert.That(leader.LinkedParameter, Is.EqualTo("Chloride_Salts.MassFraction"));
    }

    /// <summary>
    /// Локальная копия метода ResolveLinkedParametersRoles для тестирования
    /// (копирует логику из DocPassport.Editor.cs)
    /// </summary>
    private static void ResolveLinkedParametersRoles(List<QualityParameterSchema> schemas, CfgEditPassport editCfg)
    {
        var keyToSchema = new Dictionary<string, QualityParameterSchema>(System.StringComparer.OrdinalIgnoreCase);
        foreach (var s in schemas)
            keyToSchema[s.Key] = s;

        foreach (var param in editCfg.Parameters)
        {
            if (!param.Use || string.IsNullOrEmpty(param.LinkedParameter))
                continue;

            // SlaveKey имеет приоритет
            if (!string.IsNullOrEmpty(param.SlaveKey))
                continue;

            // Ведущий параметр
            if (keyToSchema.TryGetValue(param.Key, out var leaderSchema))
            {
                leaderSchema.LinkedParameter = param.LinkedParameter;
            }

            // Ведомый параметр
            if (keyToSchema.TryGetValue(param.LinkedParameter, out var followerSchema))
            {
                followerSchema.IsLinkedFollower = true;
                followerSchema.LinkedLeaderKey = param.Key;
            }
        }
    }
}
