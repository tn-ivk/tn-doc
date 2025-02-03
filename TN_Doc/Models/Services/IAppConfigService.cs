using TN.DocData;

namespace TN_Doc.Models.Services
{
    public interface IAppConfigService
    {
        CfgApp GetAppCfg();
        LastUsedTemplateListCfg GetLastUsedTemplateList();
    }
}