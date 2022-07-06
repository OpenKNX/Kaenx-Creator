using Kaenx.Creator.Classes;
using Kaenx.Creator.Viewer;
using Kaenx.DataContext.Catalog;
using Kaenx.DataContext.Local;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml.Linq;

namespace Kaenx.Creator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class ViewerWindow : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        
        private IImporter _importer;
        private CatalogContext _context;

        public ViewerWindow(IImporter importer)
        {
            InitializeComponent();
            _importer = importer;

            _context = new CatalogContext(new LocalConnectionCatalog() { Type = LocalConnectionCatalog.DbConnectionType.Memory });
            _context.Database.Migrate();

            Load();
        }

        private async void Load()
        {
            await System.Threading.Tasks.Task.Run(() => _importer.StartImport(_context)).WaitAsync(TimeSpan.FromMinutes(2));
        }
    }
}