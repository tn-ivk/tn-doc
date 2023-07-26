using System.Collections.Generic;
using TN.DocData;

namespace TN_Doc.Models.DTOs.Directories
{
    /// <summary>
    /// Модель для обновлений справочника пользовательских групп
    /// </summary>
    public sealed class UserGroupsPatches
    {
        /// <summary>
        /// Список добавленных групп пользователей
        /// </summary>
        public IEnumerable<UsersGroup> AddedUserGroups { get; set; }
       
        /// <summary>
        /// Список обновленных групп пользователей
        /// </summary>
        public IEnumerable<UsersGroup> UpdatedUserGroups { get; set; }
        
        /// <summary>
        /// Список удаленных групп пользователей
        /// </summary>
        public IEnumerable<UsersGroup> DeletedUserGroups { get; set; }
    }
}