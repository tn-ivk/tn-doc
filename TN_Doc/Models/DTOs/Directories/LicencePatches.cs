using System.Collections.Generic;
using TN.DocData;

namespace TN_Doc.Models.DTOs.Directories
{
    /// <summary>
    /// Список обновлений для справочника лицензий
    /// </summary>
    public sealed class LicencePatches
    {
        /// <summary>
        /// Список добавленных лицензий
        /// </summary>
        public IEnumerable<License> AddedLicenses { get; set; }

        /// <summary>
        /// Список удаленных лицензий
        /// </summary>
        public IEnumerable<License> DeletedLicenses { get; set; }
    }
}