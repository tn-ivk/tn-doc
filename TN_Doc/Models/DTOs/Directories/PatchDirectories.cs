namespace TN_Doc.Models.DTOs.Directories
{
    /// <summary>
    ///Список обновлений справочников
    /// </summary>
    public class PatchDirectories
    {
        // /// <summary>
        // /// Патчи для справочника групп пользователей
        // /// </summary>
        // public UserGroupsPatches UgPatches { get; set; }
        
        /// <summary>
        /// Патчи для справочника пользователей
        /// </summary>
        public UserPatches UPatches { get; set; }
        
        /// <summary>
        /// Патчи для справочника довереностей
        /// </summary>
        public LicencePatches LPatches { get; set; }
    }
}