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

        private Application _app;
        private AppVersion _version;
        private CatalogContext _context;
        private ApplicationViewModel _model;

        private Dictionary<string, int> TypeNameToId = new Dictionary<string, int>();

        public ImporterKnxProd(string filePath) => _filePath = filePath;


        public void StartImport(CatalogContext context)
        {
            _context = context;

            Kaenx.DataContext.Import.Manager.KnxProdFileManager man = new DataContext.Import.Manager.KnxProdFileManager(_filePath);
            var x = man.GetDeviceList();
            man.StartImport(x, _context);
        }

    }
}