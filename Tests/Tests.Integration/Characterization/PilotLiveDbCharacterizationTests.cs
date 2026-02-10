using Microsoft.Extensions.DependencyInjection;
using TN.Doc;
using TN_Doc.Controllers;
using TN_Doc.Models.Home;
using TN_DocGeneral.Services;
using IdDoc = TN.DocData.IdDoc;

namespace Tests.Integration.Characterization;

[TestFixture]
[Category("characterization")]
[Category("db")]
[NonParallelizable]
public class PilotLiveDbCharacterizationTests : CharacterizationTestBase
{
    [SetUp]
    public void ClearDocModuleCache()
    {
        if (GetServiceOrDefault<IDocModuleLoader>() is CachedDocModuleLoader loader)
        {
            loader.ClearCache();
        }
    }

    [Test]
    [TestCase("TableActAndPassport")]
    [TestCase("TableReport")]
    [TestCase("TableKMH_PP_Areom")]
    public void LiveDbSchema_IsAvailableForPilotTables(string tableName)
    {
        var appConfig = GetService<IAppConfigService>();
        var device = appConfig.GetDeviceCfg(0);
        Assert.That(device, Is.Not.Null, "Device 0 must exist in configuration for pilot scope");

        var channels = device.DBConnectionStrings.Where(x => x.Use).ToList();
        Assert.That(channels, Is.Not.Empty, "At least one active DB connection is required");

        using var dbService = new DBtService(channels);
        var schema = dbService.GetTableInfo(tableName);

        Assert.That(schema, Is.Not.Null.And.Not.Empty, $"DESCRIBE {tableName} returned empty schema");
        Assert.That(schema.Any(x => x.Field.Equals("id", StringComparison.OrdinalIgnoreCase)), Is.True,
            $"Table {tableName} does not contain 'id' in schema");
    }

    [Test]
    [TestCase(IdDoc.Passport)]
    [TestCase(IdDoc.Report)]
    [TestCase(IdDoc.KMH_PP_Areom)]
    public void PilotModules_GetList_ReturnsRows(IdDoc idDoc)
    {
        EnsureModuleDllPresentOrIgnore(idDoc);
        var controller = ActivatorUtilities.CreateInstance<HomeController>(Scope.ServiceProvider);

        var list = controller.GetList(new Data
        {
            IdDevice = 0,
            IdDoc = idDoc,
            DTBegin = string.Empty,
            DTEnd = string.Empty
        });

        Assert.That(list, Is.Not.Null, $"GetList returned null for {idDoc}");
        Assert.That(list.Count, Is.GreaterThan(0), $"GetList returned no rows for {idDoc}");
    }

    [Test]
    public void ReportDocIds_0_And_32_AreMappedToRuntimeDll()
    {
        var appConfig = GetService<IAppConfigService>();
        var reportDll = appConfig.GetPathToDocDll(0, IdDoc.Report);
        var reportIncompleteDll = appConfig.GetPathToDocDll(0, IdDoc.ReportIncomplete);

        Assert.That(reportDll, Is.Not.Null.And.Not.Empty, "Report DLL path is not configured");
        Assert.That(reportIncompleteDll, Is.Not.Null.And.Not.Empty, "ReportIncomplete DLL path is not configured");
        Assert.That(reportDll, Is.EqualTo(reportIncompleteDll),
            "IdDoc.Report and IdDoc.ReportIncomplete should use the same runtime DLL");

        var fullDllPath = ResolveRuntimePath(reportDll);
        if (string.IsNullOrWhiteSpace(fullDllPath) || !File.Exists(fullDllPath))
        {
            Assert.Ignore($"Report DLL not found in runtime path: {fullDllPath}");
        }
    }

    [Test]
    public void PassportModule_GetListAndGetEditDoc_ReturnsData()
    {
        EnsureModuleDllPresentOrIgnore(IdDoc.Passport);

        var controller = ActivatorUtilities.CreateInstance<HomeController>(Scope.ServiceProvider);
        var list = controller.GetList(new Data
        {
            IdDevice = 0,
            IdDoc = IdDoc.Passport,
            DTBegin = string.Empty,
            DTEnd = string.Empty
        });

        Assert.That(list, Is.Not.Null, "GetList returned null");
        Assert.That(list.Count, Is.GreaterThan(0), "GetList returned no records for Passport");

        var recordId = list.First().Id;
        Assert.That(recordId, Is.GreaterThan(0), "GetList returned invalid record id");

        var html = controller.GetDocEdit(0, IdDoc.Passport, recordId);
        Assert.That(html, Is.Not.Null.And.Not.Empty, "GetDocEdit returned empty HTML");
    }

    private static string ResolveRuntimePath(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return string.Empty;
        }

        var trimmed = relativePath.TrimStart('/', '\\');
        return Path.Combine(AppContext.BaseDirectory, trimmed);
    }

    private void EnsureModuleDllPresentOrIgnore(IdDoc idDoc)
    {
        var appConfig = GetService<IAppConfigService>();
        var dllPath = appConfig.GetPathToDocDll(0, idDoc);
        var fullDllPath = ResolveRuntimePath(dllPath);

        if (string.IsNullOrWhiteSpace(fullDllPath) || !File.Exists(fullDllPath))
        {
            Assert.Ignore($"{idDoc} DLL not found in runtime path: {fullDllPath}");
        }
    }
}
