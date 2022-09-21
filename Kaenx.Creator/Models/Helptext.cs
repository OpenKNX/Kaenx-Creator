using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows.Media;

namespace Kaenx.Creator.Models
{
    public class Helptext : INotifyPropertyChanged
    {
        private int _uid = -1;
        public int UId
        {
            get { return _uid; }
            set { _uid = value; Changed("UId"); }
        }
        
        private string _name = "dummy";
        public string Name
        {
            get { return _name; }
            set { _name = value; Changed("Name"); }
        }


        public ObservableCollection<Translation> Text {get;set;} = new ObservableCollection<Translation>();

        private bool _transText = false;
        public bool TranslationText
        {
            get { return _transText; }
            set { _transText = value; Changed("TranslationText"); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void Changed(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
