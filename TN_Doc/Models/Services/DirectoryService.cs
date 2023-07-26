using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using TN_Doc.Models.DTOs.Directories;
using TN.DocData;

namespace TN_Doc.Models.Services
{
    /// <summary>
    /// Сервис взаимодействия со справочниками
    /// </summary>
    public sealed class DirectoryService
    {
        private readonly ILogger<DirectoryService> _logger;
        private readonly FileInfo _mainCfgFile;
        private readonly object _lock;

        private Dictionarys _cacheDirectories;
        private bool _isValidCache;

        public DirectoryService(string mainCfgFilePath, ILogger<DirectoryService> logger = null)
        {
            if (string.IsNullOrEmpty(mainCfgFilePath))
                throw new ArgumentNullException(nameof(mainCfgFilePath), "Отсутствует путь главной конфигурации");
            _mainCfgFile = new FileInfo(mainCfgFilePath);
            FileNotFoundThrowExceptionHelper(_mainCfgFile);
            _logger = logger;
            _lock = new object();
        }

        public async Task<Dictionarys> GetDirectoriesAsync()
        {
            try
            {
                return await Task.Run(() =>
                {
                    lock (_lock)
                    {
                        if (_cacheDirectories != null && _isValidCache)
                            return _cacheDirectories;

                        FileNotFoundThrowExceptionHelper(_mainCfgFile);
                        _cacheDirectories = ExtractDictionarysToJson() ?? DefaultDictionarys;
                        _isValidCache = true;
                        return CreateDeepCloneDictionarys(_cacheDirectories);
                    }
                });
            }
            catch (Exception e)
            {
                _logger?.LogError(e, "Не удалось получить список справочников");
                throw;
            }
        }

        public async Task UpdateDictionariesAsync(PatchDirectories patches)
        {
            await Task.Run(() =>
            {
                lock (_lock)
                {
                    try
                    {
                        FileNotFoundThrowExceptionHelper(_mainCfgFile);
                        _isValidCache = false;
                        if (_cacheDirectories == null)
                            _cacheDirectories = ExtractDictionarysToJson() ?? DefaultDictionarys;
                        ApplyPatches(patches);
                    }
                    catch (Exception e)
                    {
                        _logger?.LogError(e, "Не удалось  список справочников");
                        throw;
                    }
                }
            });
        }

        private void ApplyPatches(PatchDirectories patches)
        {
            if (patches == null)
                return;

            if (_cacheDirectories == null)
                _cacheDirectories = ExtractDictionarysToJson() ?? DefaultDictionarys;

            ApplyUPatches(patches.UPatches, _cacheDirectories);
            ApplyLPatches(patches.LPatches, _cacheDirectories);
            WritePatchesToJson(_cacheDirectories);
        }

        [Obsolete("Не использовать. возможность редактирования групп пользователей поломает код.")]
        private void ApplyUgPatches(UserGroupsPatches patches, Dictionarys directories)
        {
            if (patches == null)
                return;

            if (patches.DeletedUserGroups != null)
            {
                foreach (var patch in patches.DeletedUserGroups)
                    directories.UsersGroup.Remove(patch);
            }

            if (patches.UpdatedUserGroups != null)
            {
                foreach (var patch in patches.UpdatedUserGroups)
                {
                    var item = directories.UsersGroup.FirstOrDefault(item => item.Id == patch.Id);
                    if (item == null)
                        throw new InvalidDataException($"Группа с идентификатором {patch.Id} не обнаружена");
                    item.Name = patch.Name;
                    item.Use = patch.Use;
                }
            }

            if (patches.AddedUserGroups != null)
            {
                foreach (var patch in patches.AddedUserGroups)
                {
                    var item = directories.UsersGroup.FirstOrDefault(item => item.Id == patch.Id);
                    if (item != null)
                        throw new InvalidDataException(
                            $"Группа с идентификатором {patch.Id} обнаружена. Необходимо чтобы у группы был уникальный идентификатор");
                    directories.UsersGroup.Add(patch);
                }
            }
        }

        private void ApplyUPatches(UserPatches patches, Dictionarys directories)
        {
            if (patches == null)
                return;

            if (patches.DeletedUsers != null)
            {
                foreach (var patchId in patches.DeletedUsers)
                {
                    var user = directories.Users.FirstOrDefault(u => u.Id == patchId);
                    if (user is null)
                        continue;
                    directories.Users.Remove(user);
                    var licences = directories.Licenses.Where(lic => lic.IdUser == patchId);
                    foreach (var lic in licences)
                        lic.IdUser = 0;
                }
            }

            if (patches.UpdatedUsers != null)
            {
                foreach (var patch in patches.UpdatedUsers)
                {
                    var item = directories.Users.FirstOrDefault(item => item.Id == patch.Id);
                    if (item == null)
                        throw new InvalidDataException($"Пользователь с идентификатором {patch.Id} не обнаружена");
                    item.Use = patch.Use;
                    item.F = patch.F;
                    item.I = patch.I;
                    item.O = patch.O;
                    item.Factory = patch.Factory;
                    item.Post = patch.Post;
                    item.IdGroup = patch.IdGroup;
                }
            }

            if (patches.AddedUsers != null)
            {
                foreach (var patch in patches.AddedUsers)
                {
                    var item = directories.Users.FirstOrDefault(item => item.Id == patch.Id);
                    if (item != null)
                        throw new InvalidDataException(
                            $"Пользователь с идентификатором {patch.Id} обнаружен. Необходимо чтобы у пользователя был уникальный идентификатор");
                    directories.Users.Add(patch);
                }
            }
        }

        private void ApplyLPatches(LicencePatches patches, Dictionarys directories)
        {
            if (patches == null)
                return;

            if (patches.DeletedLicenses != null)
            {
                foreach (var patch in patches.DeletedLicenses) 
                    directories.Licenses.Remove(patch);
            }

            if (patches.AddedLicenses != null)
            {
                foreach (var patch in patches.AddedLicenses)
                {
                    if (patch.IdUser != 0)
                    {
                        var item = directories.Licenses.FirstOrDefault((license =>
                            license.LicensesNumber == patch.LicensesNumber 
                            && license.LicensesDate == patch.LicensesDate 
                            && license.IdUser == patch.IdUser));

                        if (item != null)
                            throw new InvalidDataException(
                                $"На данного пользователя уже выдана такая довереность({patch.LicensesNumber} - {patch.LicensesDate} - {patch.IdUser}).");

                        var user = directories.Users.FirstOrDefault(user => user.Id == patch.IdUser);
                        if (user == null)
                            throw new InvalidDataException(
                                $"Пользователь с идентификатором {patch.IdUser}  не обнаружен. Необходимо указать идентификатор существующего пользователя или 0.");
                    }
                    directories.Licenses.Add(patch);
                }
            }
        }

        private void WritePatchesToJson(Dictionarys directories)
        {
            var json = File.ReadAllText(_mainCfgFile.FullName);
            var jObject = JObject.Parse(json);
            var jObjectDir = JObject.FromObject(directories);
            jObject["Doc"]?["Settings"]?["Dictionarys"]?.Replace(jObjectDir);
            File.WriteAllText(_mainCfgFile.FullName, jObject.ToString(), Encoding.Default);
        }

        /// <summary>
        /// Создание глубокой копии словарей
        /// </summary>
        /// <param name="dict">Словарь, который необходимо скопировать</param>
        /// <returns>Глубокая копия словарей объекта</returns>
        private Dictionarys CreateDeepCloneDictionarys(Dictionarys dict)
        {
            if (dict == null)
                return null;

            var cloneDict = new Dictionarys();

            if (dict.Users is not null)
            {
                cloneDict.Users = new List<Users>();
                dict.Users.ForEach(item =>
                {
                    cloneDict.Users.Add(new Users()
                    {
                        Id = item.Id,
                        Factory = item.Factory,
                        IdGroup = item.IdGroup,
                        Post = item.Post,
                        I = item.I,
                        O = item.O,
                        Use = item.Use,
                        F = item.F
                    });
                });
            }
            else
            {
                cloneDict.Users = null;
            }

            if (dict.UsersGroup is not null)
            {
                cloneDict.UsersGroup = new List<UsersGroup>();
                dict.UsersGroup.ForEach(item =>
                {
                    cloneDict.UsersGroup.Add(new UsersGroup() { Id = item.Id, Use = item.Use, Name = item.Name });
                });
            }
            else
            {
                cloneDict.UsersGroup = null;
            }

            if (dict.Licenses is not null)
            {
                cloneDict.Licenses = new List<License>();
                dict.Licenses.ForEach(item =>
                {
                    cloneDict.Licenses.Add(new License()
                    {
                        Use = item.Use,
                        IdUser = item.IdUser,
                        LicensesDate = item.LicensesDate,
                        LicensesNumber = item.LicensesNumber,
                    });
                });
            }
            else
            {
                cloneDict.Licenses = null;
            }

            return cloneDict;
        }

        private Dictionarys DefaultDictionarys =>
            new Dictionarys() { Licenses = new List<License>(), Users = new List<Users>(), UsersGroup = new List<UsersGroup>() };

        private Dictionarys ExtractDictionarysToJson()
        {
            var json = File.ReadAllText(_mainCfgFile.FullName);
            var jObject = JObject.Parse(json);
            return jObject["Doc"]?["Settings"]?["Dictionarys"]?.ToObject<Dictionarys>();
        }

        /// <summary>
        /// Вспомогательный метод проверки наличия конфигурационного файла
        /// </summary>
        /// <param name="info">Информация о файле</param>
        /// <exception cref="FileNotFoundException">Гененерируется при отсутствие файла</exception>
        private void FileNotFoundThrowExceptionHelper(FileInfo info)
        {
            if (!info.Exists)
                throw new FileNotFoundException($"Отсутствует файл с главной конфигурацией: {info.FullName}");
        }
    }
}