using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TN_Doc.Models.Services;
using TN.DocData;

namespace TN_Doc.Tests;

[TestFixture(TestName = "Тесты для проверки работы сервиса работы со словарями")]
public class DirectoryServiceTests
{
    private DirectoryService GetService(string path) => new(path, "Cfg_clone_main.json", "TestResources");

    private DirectoryService GetGoodService() => GetService("Cfg_clone.json");

    [TestCase(TestName = "#1 Проверка инциализации с корректными данными. Без генерации исключения.")]
    public void SuccessfulInitializeTest() =>
        Assert.DoesNotThrow(() =>
        {
            var _ = GetGoodService();
        });

    [TestCase(TestName = "#2 Инициализация объекта. Отсутствует путь до конфигурационного файла. Должно сгенерироватся специальное исключение.")]
    public void InitializeTestWithoutPathTest() =>
        Assert.Throws<ArgumentNullException>(() =>
        {
            var _ = GetService(null!);
        });

    [TestCase(TestName = "#3 Инициализация объекта. Указан неизвестный файл. Должно сгенерироватся специальное исключение.")]
    public void InitializeTestWithoutUnknownFile() =>
        Assert.Throws<FileNotFoundException>(() =>
        {
            var _ = GetService("TestResources/fakeCfg.json");
        });

    [TestCase(TestName = "#4 Проверка работы метода получения справочников из файла конфигурации в формате json")]
    public async Task GetDictionariesAsyncFromFileWithAllDictionaryTest()
    {
        var service = GetGoodService();
        var dictJson = await service.GetDirectoriesJsonAsync();

        Assert.Multiple(() =>
        {
            Assert.That(dictJson, Is.Not.Null);
            Assert.That(dictJson, Is.Not.Empty);
        });

        var dict = JsonConvert.DeserializeObject<Dictionarys>(dictJson);

        Assert.Multiple(() =>
        {
            Assert.That(dict!.Users, Is.Not.Null);
            Assert.That(dict!.Licenses, Is.Not.Null);
            Assert.That(dict!.UsersGroup, Is.Not.Null);

            Assert.That(dict!.Users.Count, Is.EqualTo(2));
            Assert.That(dict!.UsersGroup.Count, Is.EqualTo(2));
            Assert.That(dict!.Licenses.Count, Is.EqualTo(2));
        });
    }

    [TestCase(TestName = "#5 Проверка работы метода замены справочников новыми данными, через json из файла конфигурации в формате json")]
    public async Task SetDictionariesAsyncToJsonFile()
    {
        var service = GetGoodService();
        var dictJson = await service.GetDirectoriesJsonAsync();
        Assert.Multiple(() =>
        {
            Assert.That(dictJson, Is.Not.Null);
            Assert.That(dictJson, Is.Not.Empty);
        });
        var dict = JsonConvert.DeserializeObject<Dictionarys>(dictJson);
        var user = dict!.Users.FirstOrDefault();
        user!.Id = 100;
        var modifJson = JsonConvert.SerializeObject(dict);
        Assert.DoesNotThrowAsync(async () => { await service.SetDirectoriesFromJsonAsync(modifJson); });


        var ndictJson = await service.GetDirectoriesJsonAsync();
        Assert.Multiple(() =>
        {
            Assert.That(dictJson, Is.Not.Null);
            Assert.That(dictJson, Is.Not.Empty);
        });
        var ndict = JsonConvert.DeserializeObject<Dictionarys>(dictJson);
        var nuser = dict.Users.FirstOrDefault(item => item.Id == 100);
        Assert.That(nuser, Is.Not.Null);
    }

    [TestCase(TestName = "#6 Получение конфигураций по паспортам качества. В результате должна получится информация об девайсе и")]
    public async Task GetQPCfgTest()
    {
        var service = GetGoodService();
        var result = await service.GetQualityPassportConfigs();
        Assert.That(result, Is.Not.Null);
        var jsonResult = JObject.Parse(result);
        Assert.Multiple(() =>
        {
            Assert.That(jsonResult["QpsInfo"]!.Count(), Is.EqualTo(4));
            foreach (var device in jsonResult["QpsInfo"]!)
            {
                Assert.That(string.IsNullOrEmpty(device["EditConfigFilePath"]!.ToString()), Is.False);
                Assert.That(string.IsNullOrEmpty(device["Name"]!.ToString()), Is.False);
                Assert.That(string.IsNullOrEmpty(device["Methods"]!.ToString()), Is.False);
                Assert.That(string.IsNullOrEmpty(device["Parameters"]!.ToString()), Is.False);
            }

            Assert.That(jsonResult["QpsInfo"]![0]!["Name"]!.ToString(), Is.EqualTo("Паспорт качества на экспорт"));
        });
    }


    [TestCase(TestName = "#7 Запись модифицированного jsona в файлик")]
    public async Task SetModifQpJsonTest()
    {
        var randomKey = Guid.NewGuid();
        var service = GetGoodService();
        var result = await service.GetQualityPassportConfigs();
        Assert.That(result, Is.Not.Null);
        var jResult = JObject.Parse(result);
        jResult["QpsInfo"]!.First()["Methods"]![0]!["RandomKey"] = randomKey;
        jResult["QpsInfo"]!.First()["Methods"]![0]!["Id"] = 100;

        await service.SetQpConfigFromJsonAsync(jResult.ToString());

        var nResult = await service.GetQualityPassportConfigs();
        var njResult = JObject.Parse(nResult);
        
        var fObject = njResult["QpsInfo"]!.First();
        Assert.That(100.ToString(), Is.EqualTo(fObject["Methods"]![0]!["Id"]!.ToString()));
        Assert.That(randomKey.ToString(), Is.EqualTo(fObject["Methods"]![0]!["RandomKey"]!.ToString()));
        Assert.Pass("Все ок");
    }
}