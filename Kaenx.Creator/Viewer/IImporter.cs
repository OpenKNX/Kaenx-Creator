using Kaenx.DataContext.Catalog;
using System.Collections.Generic;

namespace Kaenx.Creator.Viewer
{
    public interface IImporter
    {
        void StartImport(CatalogContext context);
        List<string> GetLanguages();
        void SetLanguage(string langCode);
        List<ModuleModel> Modules { get; set; }
    }
}