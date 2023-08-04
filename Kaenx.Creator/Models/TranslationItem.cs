using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Kaenx.Creator.Models
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class TranslationItem
    {
        public string Name { get; set; }
        public string Group { get; set; }
        public string SubGroup { get; set; }
        public ObservableCollection<Translation> Text { get; set; }
    }
}