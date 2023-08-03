using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Kaenx.Creator.Models
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class TranslationTab : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public ObservableCollection<TranslationItem> Items { get; set; } = new ObservableCollection<TranslationItem>();


        private string _name = "";
        public string Name {
            get { return _name; }
            set { _name = value; Changed("Name"); }
        }

        private void Changed(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}