using System.Collections.Generic;
using Moq;
using TN.DocData;
using TN_DocGeneral.Dictionaries;
using TN_DocGeneral.Services;

namespace Tests.Fixtures;

/// <summary>
/// Хелпер для настройки моков IAppConfigService в тестах
/// </summary>
public static class MockConfigHelper
{
    /// <summary>
    /// Настраивает мок IAppConfigService для тестов библиотек документов
    /// </summary>
    /// <param name="mockAppConfig">Мок IAppConfigService для настройки</param>
    /// <param name="idDevice">ID устройства (по умолчанию 1)</param>
    /// <param name="customizeRoot">Опциональный делегат для кастомизации Root</param>
    /// <param name="customizeDevice">Опциональный делегат для кастомизации Device</param>
    public static void SetupMockAppConfig(
        Mock<IAppConfigService> mockAppConfig,
        int idDevice = 1,
        System.Action<Root> customizeRoot = null,
        System.Action<Device> customizeDevice = null)
    {
        // Создаем минимальную валидную структуру Root
        var root = new Root
        {
            Doc = new Doc
            {
                Version = "1.0",
                Settings = new Settings
                {
                    General = new General
                    {
                        ObjType = 1,
                        NefType = 1,
                        PageSettings = new PageSettings
                        {
                            PaperWidth = 210,
                            PaperHeight = 297,
                            TopMargin = 10,
                            BottomMargin = 10,
                            LeftMargin = 15,
                            RightMargin = 15
                        },
                        FileNameForExportDoc = "export",
                        ProtocolNumber = 1
                    },
                    Header = new Header(),
                    Data = new Data(),
                    Footer = new Footer(),
                    Dictionarys = new Dictionarys
                    {
                        Users = new List<Users>(),
                        UsersGroup = new List<UsersGroup>(),
                        Licenses = new List<License>(),
                        BIKs = new List<BIK>(),
                        Directions = new List<Direction>()
                    }
                },
                DataIVK = new DataIVK(),
                DataArm = new DataArm()
            }
        };

        // Применяем кастомизацию Root если она передана
        customizeRoot?.Invoke(root);

        // Создаем минимальную валидную структуру Device
        var device = new Device
        {
            Use = true,
            IdDevice = idDevice,
            Name = $"Test Device {idDevice}",
            Description = "Test device for unit tests",
            Docs = new List<Document>(),
            DBConnectionStrings = new List<DBConnectionString>
            {
                new DBConnectionString
                {
                    Use = true,
                    Server = "localhost",
                    Database = "test_db",
                    Userid = "test_user",
                    Password = "test_password",
                    ConnectionTimeout = 30
                }
            },
            Elis = new Elis { Use = false },
            UsedSI = new UsedSI
            {
                UsedPR = true,
                UsedPP = true,
                UsedPVL = true,
                UsedPVS = true,
                UsedSecondSI_PP = true,
                UsedSecondSI_PVL = true,
                UsedSecondSI_PVS = true
            },
            UsedProcedureTypePR = new UsedProcedureTypePR
            {
                UseKmhPU = true,
                UseKmhPR = true
            },
            OpcConnectionSettings = new OpcConnectionSettings
            {
                Type = OpcType.UA,
                DaSettings = null,
                UaSettings = null
            },
            InvalidChars = new List<char>()
        };

        // Применяем кастомизацию Device если она передана
        customizeDevice?.Invoke(device);

        // Настраиваем моки для методов GetCfg и GetDeviceCfg
        mockAppConfig.Setup(x => x.GetCfg()).Returns(root);
        mockAppConfig.Setup(x => x.GetDeviceCfg(idDevice)).Returns(device);

        // Настраиваем моки для путей к файлам конфигурации и шаблонов
        mockAppConfig.Setup(x => x.GetPathConfigFile(It.IsAny<int>(), It.IsAny<IdDoc>()))
            .Returns((int deviceId, IdDoc docId) => $"Cfg/Cfg{docId}.json");

        mockAppConfig.Setup(x => x.GetPathEditConfigFile(It.IsAny<int>(), It.IsAny<IdDoc>()))
            .Returns((int deviceId, IdDoc docId) => $"Cfg/CfgEdit{docId}.json");

        mockAppConfig.Setup(x => x.GetPathTemplateFile(It.IsAny<int>(), It.IsAny<IdDoc>()))
            .Returns((int deviceId, IdDoc docId) => $"Doc/{docId}.frx");
    }

    /// <summary>
    /// Создает настроенный мок IAppConfigService для тестов
    /// </summary>
    /// <param name="idDevice">ID устройства (по умолчанию 1)</param>
    /// <param name="customizeRoot">Опциональный делегат для кастомизации Root</param>
    /// <param name="customizeDevice">Опциональный делегат для кастомизации Device</param>
    /// <returns>Настроенный мок IAppConfigService</returns>
    public static Mock<IAppConfigService> CreateMockAppConfig(
        int idDevice = 1,
        System.Action<Root> customizeRoot = null,
        System.Action<Device> customizeDevice = null)
    {
        var mock = new Mock<IAppConfigService>();
        SetupMockAppConfig(mock, idDevice, customizeRoot, customizeDevice);
        return mock;
    }
}
