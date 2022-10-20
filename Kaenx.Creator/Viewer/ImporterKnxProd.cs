using Kaenx.Creator.Models;
using Kaenx.DataContext.Catalog;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Kaenx.Creator.Viewer
{
    public class ImporterKnxProd : IImporter
    {
        private string _filePath = "";

        private CatalogContext _context;
        private Kaenx.DataContext.Import.Manager.KnxProdFileManager _man;

        private Dictionary<string, int> TypeNameToId = new Dictionary<string, int>();
        
        public List<ModuleModel> Modules { get; set; } = new List<ModuleModel>();

        public ImporterKnxProd(string filePath)
        {
            _filePath = filePath;
            _man = new DataContext.Import.Manager.KnxProdFileManager(_filePath);
        }


        public void StartImport(CatalogContext context)
        {
            _context = context;

            var x = _man.GetDeviceList();
            _man.StartImport(x, _context);
        }

        public List<string> GetLanguages()
        {
            return _man.GetLanguages();
        }

        public void SetLanguage(string langCode)
        {
            _man.SetLanguage(langCode);
        }

    }
}