using Kaenx.DataContext.Catalog;

namespace Kaenx.Creator.Viewer
{
    public interface IImporter
    {
        void StartImport(CatalogContext context);
    }
}