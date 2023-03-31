using Kaenx.Creator.Classes;
using Kaenx.Creator.Models;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO.Compression;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using System.Diagnostics;

namespace Kaenx.Creator.Controls
{
    public partial class IconView : UserControl, INotifyPropertyChanged
    {
        public static readonly System.Windows.DependencyProperty GeneralProperty = System.Windows.DependencyProperty.Register("General", typeof(ModelGeneral), typeof(IconView), new System.Windows.PropertyMetadata(null));
        public ModelGeneral General {
            get { return (ModelGeneral)GetValue(GeneralProperty); }
            set { SetValue(GeneralProperty, value); }
        }

        public IconView()
		{
            InitializeComponent();
        }

        private void ClickAdd(object sender, System.Windows.RoutedEventArgs e)
        {
            OpenFileDialog diag = new OpenFileDialog();
            diag.Title = Properties.Messages.icon_add_title;
            diag.Filter = Properties.Messages.icon_change_filter + " (PNG, JPG)|*.png;*.jpg";
            if(diag.ShowDialog() == true)
            {
                Icon icon = new Icon();
                icon.UId = AutoHelper.GetNextFreeUId(General.Icons);
                icon.Data = System.IO.File.ReadAllBytes(diag.FileName);
                icon.Name = Path.GetFileNameWithoutExtension(diag.FileName);

                General.Icons.Add(icon);
            }
        }

        private void ClickDelete(object sender, System.Windows.RoutedEventArgs e)
        {   
            Icon icon = IconsList.SelectedItem as Icon;
            List<ParameterType> types = new List<ParameterType>();

            /*foreach(Models.Application app in General.Applications)
                foreach(AppVersion vers in app.Versions)
                    foreach(ParameterType type in vers.ParameterTypes)
                        if(type.Type == ParameterTypes.Picture && type.BaggageObject == bag)
                            types.Add(type);
*/
//TODO implement
            if(types.Count > 0)
            {
                var result = MessageBox.Show(string.Format(Properties.Messages.icon_delete_error, types.Count), Properties.Messages.icon_delete_error_title, MessageBoxButton.YesNo);
                if(result == MessageBoxResult.No) return;
            }

            General.Icons.Remove(icon);
        }

        private void ClickChangeFile(object sender, System.Windows.RoutedEventArgs e)
        {
            OpenFileDialog diag = new OpenFileDialog();
            diag.Title = Properties.Messages.icon_change_title;
            diag.Filter = Properties.Messages.icon_change_filter + " (PNG)|*.png";
            if(diag.ShowDialog() == true)
            {
                Icon icon = (sender as Button).DataContext as Icon;
                icon.Data = System.IO.File.ReadAllBytes(diag.FileName);
                System.Windows.MessageBox.Show(Properties.Messages.icon_change_success, Properties.Messages.icon_change_title);
                IconsList.SelectedItem = null;
                IconsList.SelectedItem = icon;
            }
        }
        
        private void ClickImport(object sender, System.Windows.RoutedEventArgs e)
        {
            OpenFileDialog diag = new OpenFileDialog();
            diag.Title = Properties.Messages.icon_import_title;
            diag.Filter = Properties.Messages.icon_import_filter + " (*.ae-icons)|*.ae-icons|ZIP Datei (*.zip)|*.zip";
            if(diag.ShowDialog() == true)
            {   
                if(MessageBox.Show(Properties.Messages.icon_import_prompt, "", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                    General.Icons.Clear();

                if(diag.FileName.EndsWith(".ae-icons"))
                {
                    var x = Newtonsoft.Json.JsonConvert.DeserializeObject<ObservableCollection<Icon>>(File.ReadAllText(diag.FileName));
                    
                    foreach(Icon icon in x)
                        General.Icons.Add(icon);
                } else {
                    ZipArchive zip = ZipFile.Open(diag.FileName, ZipArchiveMode.Read, System.Text.Encoding.GetEncoding(850));
                    
                    foreach(ZipArchiveEntry entry in zip.Entries)
                    {
                        Icon icon = new Icon()
                        {
                            UId = AutoHelper.GetNextFreeUId(General.Icons),
                            Name = entry.Name.Substring(0, entry.Name.LastIndexOf("."))
                        };

                        using(Stream s = entry.Open())
                        {
                            using(MemoryStream ms = new MemoryStream())
                            {
                                s.CopyTo(ms);
                                icon.Data = ms.ToArray();
                            }
                        }

                        General.Icons.Add(icon);
                    }

                    zip.Dispose();
                }
            }
        }

        private void ClickExport(object sender, System.Windows.RoutedEventArgs e)
        {
            SaveFileDialog diag = new SaveFileDialog();
            diag.FileName = General.ProjectName + "_Icons";
            diag.Title = Properties.Messages.icon_export_title;
            diag.Filter = Properties.Messages.icon_export_filter + " (*.ae-icons)|*.ae-icons|ZIP Datei (*.zip)|*.zip";
            
            if(diag.ShowDialog() == true)
            {
                if(diag.FileName.EndsWith(".ae-icons"))
                {
                    System.IO.File.WriteAllText(diag.FileName, Newtonsoft.Json.JsonConvert.SerializeObject(General.Icons));
                } else {
                    if(File.Exists(diag.FileName)) File.Delete(diag.FileName);
                    using (var stream = new FileStream(diag.FileName, FileMode.Create))
                    using (var archive = new ZipArchive(stream , ZipArchiveMode.Create, false,  System.Text.Encoding.GetEncoding(850)))
                    {
                        foreach(Icon icon in General.Icons)
                        {
                            ZipArchiveEntry entry = archive.CreateEntry(icon.Name + ".png");
                            using(Stream s = entry.Open())
                            {
                                s.Write(icon.Data, 0, icon.Data.Length);
                            }
                        }
                    }
                }
                
            }
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            // for .NET Core you need to add UseShellExecute = true
            // see https://docs.microsoft.com/dotnet/api/system.diagnostics.processstartinfo.useshellexecute#property-value
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
        }


        public event PropertyChangedEventHandler PropertyChanged;
        private void Changed(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
