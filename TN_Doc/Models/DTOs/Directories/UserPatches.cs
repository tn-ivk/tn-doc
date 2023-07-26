using System.Collections.Generic;
using TN.DocData;

namespace TN_Doc.Models.DTOs.Directories
{
    /// <summary>
    /// Модель патчей для справочников пользователей
    /// </summary>
    public class UserPatches
    {
        /// <summary>
        /// Список добавленных пользователи
        /// </summary>
        public IEnumerable<Users> AddedUsers { get; set; }
       
        /// <summary>
        /// Список обновленных пользователи
        /// </summary>
        public IEnumerable<Users> UpdatedUsers { get; set; }
        
        /// <summary>
        /// Список удаленных пользователи
        /// </summary>
        public IEnumerable<int> DeletedUsers { get; set; }
    }
}