using NUnit.Framework.Interfaces;
using TN_Doc.Models.DTOs.Directories;
using TN_Doc.Models.Services;
using TN.DocData;

namespace TN_Doc.Tests;

[TestFixture(TestName = "Тесты для проверки работы сервиса работы со словарями")]
public class DirectoryServiceTests
{
    private DirectoryService GetService(string path) => new(path);

    private DirectoryService GetGoodService() => GetService("TestResources/Cfg_clone.json");

    [TestCase(TestName = "#1 Проверка инциализации с корректными данными. Без генерации исключения.")]
    public void SuccessfulInitializeTest() =>
        Assert.DoesNotThrow(() =>
        {
            var _ = GetGoodService();
        });

    [TestCase(TestName
        = "#2 Инициализация объекта. Отсутствует путь до конфигурационного файла. Должно сгенерироватся специальное исключение.")]
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

    [TestCase(TestName = "#4 Проверка работы метода получения справочников из файла конфигурации")]
    public async Task GetDictionariesAsyncFromFileWithAllDictionaryTest()
    {
        var service = GetGoodService();
        var dict = await service.GetDirectoriesAsync();

        Assert.Multiple(() =>
        {
            Assert.That(dict, Is.Not.Null);
            Assert.That(dict.Users, Is.Not.Null);
            Assert.That(dict.Licenses, Is.Not.Null);
            Assert.That(dict.UsersGroup, Is.Not.Null);

            Assert.That(dict.Users.Count, Is.EqualTo(2));
            Assert.That(dict.UsersGroup.Count, Is.EqualTo(2));
            Assert.That(dict.Licenses.Count, Is.EqualTo(2));
        });
    }

    [TestCase(TestName
        = "#5 Проверка работы метода получения справочников из файла конфигурации без справочников.Должен вернутся пустой список словарей")]
    public async Task GetDictionariesAsyncFromFileWithoutDictionariesTest()
    {
        var service = GetService("TestResources/Cfg_clone_without_dictionaries.json");
        var dict = await service.GetDirectoriesAsync();
        Assert.Multiple(() =>
        {
            Assert.That(dict, Is.Not.Null);
            Assert.That(dict.Users, Is.Not.Null);
            Assert.That(dict.Licenses, Is.Not.Null);
            Assert.That(dict.UsersGroup, Is.Not.Null);

            Assert.That(dict.Users.Count, Is.EqualTo(0));
            Assert.That(dict.UsersGroup.Count, Is.EqualTo(0));
            Assert.That(dict.Licenses.Count, Is.EqualTo(0));
        });
    }

    [TestCase(TestName = "#6 Добавление пользователя в справочник.")]
    public async Task AddUserPatchTest()
    {
        var service = GetService("TestResources/Cfg_clone_add_user_patch.json");
        var dict = await service.GetDirectoriesAsync();
        Assert.Multiple(() =>
        {
            Assert.That(dict, Is.Not.Null);
            Assert.That(dict.Users, Is.Not.Null);
            Assert.That(dict.Licenses, Is.Not.Null);
            Assert.That(dict.UsersGroup, Is.Not.Null);

            Assert.That(dict.Users.Count, Is.EqualTo(2));
            Assert.That(dict.UsersGroup.Count, Is.EqualTo(2));
            Assert.That(dict.Licenses.Count, Is.EqualTo(2));
        });

        Assert.DoesNotThrowAsync(async () =>
        {
            await service.UpdateDictionariesAsync(new PatchDirectories()
            {
                UPatches = new UserPatches()
                {
                    AddedUsers = new List<Users>()
                    {
                        new Users()
                        {
                            F = "test1",
                            Factory = "test1",
                            O = "test1",
                            I = "test1",
                            Id = 3,
                            Post = "test1",
                            IdGroup = 1,
                            Use = false,
                        },
                        new Users()
                        {
                            F = "test2",
                            Factory = "test2",
                            O = "test2",
                            I = "test1",
                            Id = 4,
                            Post = "test1",
                            IdGroup = 1,
                            Use = false,
                        },
                    }
                }
            });
        });

        var dictAfterPatch = await service.GetDirectoriesAsync();
        Assert.Multiple(() =>
        {
            Assert.That(dictAfterPatch, Is.Not.Null);
            Assert.That(dictAfterPatch.Users, Is.Not.Null);
            Assert.That(dictAfterPatch.Licenses, Is.Not.Null);
            Assert.That(dictAfterPatch.UsersGroup, Is.Not.Null);

            Assert.That(dictAfterPatch.Users.Count, Is.EqualTo(4));
            Assert.That(dictAfterPatch.UsersGroup.Count, Is.EqualTo(2));
            Assert.That(dictAfterPatch.Licenses.Count, Is.EqualTo(2));
        });
    }

    [TestCase(TestName = "#7 Патч на удаление пользователей из справочника.")]
    public async Task DeleteUserPatchTest()
    {
        var service = GetService("TestResources/Cfg_clone_delete_user_patch.json");
        var dict = await service.GetDirectoriesAsync();
        Assert.Multiple(() =>
        {
            Assert.That(dict, Is.Not.Null);
            Assert.That(dict.Users, Is.Not.Null);
            Assert.That(dict.Licenses, Is.Not.Null);
            Assert.That(dict.UsersGroup, Is.Not.Null);

            Assert.That(dict.Users.Count, Is.EqualTo(2));
            Assert.That(dict.UsersGroup.Count, Is.EqualTo(2));
            Assert.That(dict.Licenses.Count, Is.EqualTo(2));
        });

        Assert.DoesNotThrowAsync(async () =>
        {
            await service.UpdateDictionariesAsync(new PatchDirectories()
            {
                UPatches = new UserPatches() { DeletedUsers = new List<int>() { 1 } }
            });
        });

        var dictAfterPatch = await service.GetDirectoriesAsync();
        Assert.Multiple(() =>
        {
            Assert.That(dictAfterPatch, Is.Not.Null);
            Assert.That(dictAfterPatch.Users, Is.Not.Null);
            Assert.That(dictAfterPatch.Licenses, Is.Not.Null);
            Assert.That(dictAfterPatch.UsersGroup, Is.Not.Null);

            Assert.That(dictAfterPatch.Users.Count, Is.EqualTo(1));
            Assert.That(dictAfterPatch.UsersGroup.Count, Is.EqualTo(2));
            Assert.That(dictAfterPatch.Licenses.Count, Is.EqualTo(2));
        });
    }

    [TestCase(TestName = "#8 Патч на обновление пользователя.")]
    public async Task UpdateUserPatchTest()
    {
        var service = GetService("TestResources/Cfg_clone_update_user_patch.json");
        var dict = await service.GetDirectoriesAsync();
        Assert.Multiple(() =>
        {
            Assert.That(dict, Is.Not.Null);
            Assert.That(dict.Users, Is.Not.Null);
            Assert.That(dict.Licenses, Is.Not.Null);
            Assert.That(dict.UsersGroup, Is.Not.Null);

            Assert.That(dict.Users.Count, Is.EqualTo(2));
            Assert.That(dict.UsersGroup.Count, Is.EqualTo(2));
            Assert.That(dict.Licenses.Count, Is.EqualTo(2));
        });

        var patches = new PatchDirectories()
        {
            UPatches = new UserPatches()
            {
                UpdatedUsers = new List<Users>()
                {
                    new()
                    {
                        Id = 1,
                        IdGroup = 2,
                        F = "Кондратьев",
                        I = "Илья",
                        O = "Евгеньевич",
                        Factory = "АО «Газпромнефть-ННГ»",
                        Post = "Главный ученый этой деревни",
                        Use = true,
                    }
                }
            }
        };

        Assert.DoesNotThrowAsync(async () => { await service.UpdateDictionariesAsync(patches); });

        var dictAfterPatch = await service.GetDirectoriesAsync();
        Assert.Multiple(() =>
        {
            Assert.That(dictAfterPatch, Is.Not.Null);
            Assert.That(dictAfterPatch.Users, Is.Not.Null);
            Assert.That(dictAfterPatch.Licenses, Is.Not.Null);
            Assert.That(dictAfterPatch.UsersGroup, Is.Not.Null);

            Assert.That(dictAfterPatch.Users.Count, Is.EqualTo(2));
            Assert.That(dictAfterPatch.UsersGroup.Count, Is.EqualTo(2));
            Assert.That(dictAfterPatch.Licenses.Count, Is.EqualTo(2));
        });

        var user = dictAfterPatch.Users.FirstOrDefault(item => item.Id == 1);

        Assert.Multiple(() =>
        {
            Assert.That(user, Is.Not.Null);
            Assert.That(user!.Id, Is.EqualTo(1));
            Assert.That(user!.Use, Is.EqualTo(patches.UPatches.UpdatedUsers.FirstOrDefault(item => item.Id == user.Id)!.Use));
            Assert.That(user!.F, Is.EqualTo(patches.UPatches.UpdatedUsers.FirstOrDefault(item => item.Id == user.Id)!.F));
            Assert.That(user!.O, Is.EqualTo(patches.UPatches.UpdatedUsers.FirstOrDefault(item => item.Id == user.Id)!.O));
        });
    }

    [TestCase(TestName = "#9 Патч на добавление новых лицензий.")]
    public async Task AddLicencePatchWithNormalUserIdPatchTest()
    {
        var service = GetService("TestResources/Cfg_clone_add_licences_patch.json");
        var dict = await service.GetDirectoriesAsync();
        Assert.Multiple(() =>
        {
            Assert.That(dict, Is.Not.Null);
            Assert.That(dict.Users, Is.Not.Null);
            Assert.That(dict.Licenses, Is.Not.Null);
            Assert.That(dict.UsersGroup, Is.Not.Null);

            Assert.That(dict.Users.Count, Is.EqualTo(2));
            Assert.That(dict.UsersGroup.Count, Is.EqualTo(2));
            Assert.That(dict.Licenses.Count, Is.EqualTo(2));
        });

        var patches = new PatchDirectories()
        {
            LPatches = new LicencePatches()
            {
                AddedLicenses = new List<License>()
                {
                    new() { Use = false, IdUser = 1, LicensesDate = "10.0.2023", LicensesNumber = "1" },
                    new() { Use = true, IdUser = 2, LicensesDate = "10.0.2023", LicensesNumber = "2 " }
                }
            }
        };

        Assert.DoesNotThrowAsync(async () => { await service.UpdateDictionariesAsync(patches); });

        var dictAfterPatch = await service.GetDirectoriesAsync();
        Assert.Multiple(() =>
        {
            Assert.That(dictAfterPatch, Is.Not.Null);
            Assert.That(dictAfterPatch.Users, Is.Not.Null);
            Assert.That(dictAfterPatch.Licenses, Is.Not.Null);
            Assert.That(dictAfterPatch.UsersGroup, Is.Not.Null);

            Assert.That(dictAfterPatch.Users.Count, Is.EqualTo(2));
            Assert.That(dictAfterPatch.UsersGroup.Count, Is.EqualTo(2));
            Assert.That(dictAfterPatch.Licenses.Count, Is.EqualTo(4));
        });
    }


    [TestCase(TestName = "#10 Патч на добавление новых лицензий с неизвестными пользаками.")]
    public async Task AddLicencePatchWithUnknownUserIdPatchTest()
    {
        var service = GetService("TestResources/Cfg_clone_add_user_patch.json");
        var dict = await service.GetDirectoriesAsync();
        Assert.Multiple(() =>
        {
            Assert.That(dict, Is.Not.Null);
            Assert.That(dict.Users, Is.Not.Null);
            Assert.That(dict.Licenses, Is.Not.Null);
            Assert.That(dict.UsersGroup, Is.Not.Null);

            Assert.That(dict.Users.Count, Is.EqualTo(2));
            Assert.That(dict.UsersGroup.Count, Is.EqualTo(2));
            Assert.That(dict.Licenses.Count, Is.EqualTo(2));
        });

        var patches = new PatchDirectories()
        {
            LPatches = new LicencePatches()
            {
                AddedLicenses = new List<License>()
                {
                    new() { Use = false, IdUser = 10, LicensesDate = "10.0.2023", LicensesNumber = "1" },
                    new() { Use = true, IdUser = 20, LicensesDate = "10.0.2023", LicensesNumber = "2 " }
                }
            }
        };

        Assert.ThrowsAsync<InvalidDataException>(async () => { await service.UpdateDictionariesAsync(patches); });
    }

    [TestCase(TestName = "#9 Патч на удаление старых лицензий.")]
    public async Task DeleteLicencePatchPatchTest()
    {
        var service = GetService("TestResources/Cfg_clone_delete_lic_patch .json");
        var dict = await service.GetDirectoriesAsync();
        Assert.Multiple(() =>
        {
            Assert.That(dict, Is.Not.Null);
            Assert.That(dict.Users, Is.Not.Null);
            Assert.That(dict.Licenses, Is.Not.Null);
            Assert.That(dict.UsersGroup, Is.Not.Null);

            Assert.That(dict.Users.Count, Is.EqualTo(2));
            Assert.That(dict.UsersGroup.Count, Is.EqualTo(2));
            Assert.That(dict.Licenses.Count, Is.EqualTo(2));
        });

        var patches = new PatchDirectories() { LPatches = new LicencePatches() { DeletedLicenses = new List<int>() { 1 } } };

        Assert.DoesNotThrowAsync(async () => { await service.UpdateDictionariesAsync(patches); });

        var dictAfterPatch = await service.GetDirectoriesAsync();
        Assert.Multiple(() =>
        {
            Assert.That(dictAfterPatch, Is.Not.Null);
            Assert.That(dictAfterPatch.Users, Is.Not.Null);
            Assert.That(dictAfterPatch.Licenses, Is.Not.Null);
            Assert.That(dictAfterPatch.UsersGroup, Is.Not.Null);

            Assert.That(dictAfterPatch.Users.Count, Is.EqualTo(2));
            Assert.That(dictAfterPatch.UsersGroup.Count, Is.EqualTo(2));
            Assert.That(dictAfterPatch.Licenses.Count, Is.EqualTo(1));
        });
    }


    [TestCase(TestName = "#10 Патч на обновление старых лицензий довереностей.")]
    public async Task UpdateLicencePatchTest()
    {
        var service = GetService("TestResources/Cfg_clone_update_licences.json");
        var dict = await service.GetDirectoriesAsync();
        Assert.Multiple(() =>
        {
            Assert.That(dict, Is.Not.Null);
            Assert.That(dict.Users, Is.Not.Null);
            Assert.That(dict.Licenses, Is.Not.Null);
            Assert.That(dict.UsersGroup, Is.Not.Null);

            Assert.That(dict.Users.Count, Is.EqualTo(2));
            Assert.That(dict.UsersGroup.Count, Is.EqualTo(2));
            Assert.That(dict.Licenses.Count, Is.EqualTo(2));
        });

        var patches = new PatchDirectories()
        {
            LPatches = new LicencePatches()
            {
                UpdatedLicenses = new List<License>()
                {
                    new()
                    {
                        Use = false,
                        LicensesDate = "12.02.2019",
                        LicensesNumber = "1",
                        IdUser = 1,
                        Id = 1
                    }
                }
            }
        };

        Assert.DoesNotThrowAsync(async () => { await service.UpdateDictionariesAsync(patches); });

        var dictAfterPatch = await service.GetDirectoriesAsync();
        Assert.Multiple(() =>
        {
            Assert.That(dictAfterPatch, Is.Not.Null);
            Assert.That(dictAfterPatch.Users, Is.Not.Null);
            Assert.That(dictAfterPatch.Licenses, Is.Not.Null);
            Assert.That(dictAfterPatch.UsersGroup, Is.Not.Null);

            Assert.That(dictAfterPatch.Users.Count, Is.EqualTo(2));
            Assert.That(dictAfterPatch.UsersGroup.Count, Is.EqualTo(2));
            Assert.That(dictAfterPatch.Licenses.Count, Is.EqualTo(2));
        });

        var lic = dictAfterPatch.Licenses.FirstOrDefault(item => item.Id == 1);

        var patch = dictAfterPatch.Licenses.FirstOrDefault(item => item.Id == 1);
        
        
        Assert.Multiple(() =>
        {
            Assert.That(lic,Is.Not.Null);
            Assert.That(patch,Is.Not.Null);
            Assert.That(lic!.Id,Is.EqualTo(patch!.Id));
            Assert.That(lic!.IdUser,Is.EqualTo(patch!.IdUser));
            Assert.That(lic!.LicensesDate,Is.EqualTo(patch!.LicensesDate));
            Assert.That(lic!.LicensesNumber,Is.EqualTo(patch!.LicensesNumber));
        });
    }


    [TestCase(TestName
        = "#11 Во время применения патчей произошла ошибка. Должны быть отменены все патчи. Список словарей должен отстаться таким же как и был до применения патчей.")]
    public async Task TransactionFailTest()
    {
        var service = GetService("TestResources/Cfg_clone_transaction_error.json");
        var dict = await service.GetDirectoriesAsync();
        Assert.Multiple(() =>
        {
            Assert.That(dict, Is.Not.Null);
            Assert.That(dict.Users, Is.Not.Null);
            Assert.That(dict.Licenses, Is.Not.Null);
            Assert.That(dict.UsersGroup, Is.Not.Null);

            Assert.That(dict.Users.Count, Is.EqualTo(2));
            Assert.That(dict.UsersGroup.Count, Is.EqualTo(2));
            Assert.That(dict.Licenses.Count, Is.EqualTo(2));
        });

        Assert.ThrowsAsync<InvalidDataException>(async () =>
        {
            await service.UpdateDictionariesAsync(new PatchDirectories()
            {
                UPatches = new UserPatches()
                {
                    AddedUsers = new List<Users>()
                    {
                        new Users()
                        {
                            F = "test1",
                            Factory = "test1",
                            O = "test1",
                            I = "test1",
                            Id = 1,
                            Post = "test1",
                            IdGroup = 1,
                            Use = false,
                        },
                        new Users()
                        {
                            F = "test2",
                            Factory = "test2",
                            O = "test2",
                            I = "test1",
                            Id = 4,
                            Post = "test1",
                            IdGroup = 1,
                            Use = false,
                        },
                    }
                }
            });
        });
        var dictAfterPatch = await service.GetDirectoriesAsync();
        Assert.Multiple(() =>
        {
            Assert.That(dictAfterPatch, Is.Not.Null);
            Assert.That(dictAfterPatch.Users, Is.Not.Null);
            Assert.That(dictAfterPatch.Licenses, Is.Not.Null);
            Assert.That(dictAfterPatch.UsersGroup, Is.Not.Null);

            Assert.That(dictAfterPatch.Users.Count, Is.EqualTo(2));
            Assert.That(dictAfterPatch.UsersGroup.Count, Is.EqualTo(2));
            Assert.That(dictAfterPatch.Licenses.Count, Is.EqualTo(2));
        });
    }
}