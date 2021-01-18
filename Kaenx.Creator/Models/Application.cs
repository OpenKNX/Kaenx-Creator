using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.ObjectModel;
using Newtonsoft.Json;
using System.ComponentModel;

namespace Kaenx.Creator.Models
{
    public class Application : INotifyPropertyChanged
    {
        private string _name = "Dummy";
        public string Name
        {
            get { return _name; }
            set { _name = value; Changed("Name"); Changed("NameText"); }
        }

        private int _number = 1;
        public int Number
        {
            get { return _number; }
            set { _number = value; Changed("Number"); Changed("NameText"); }
        }

        public string NameText
        {
            get { return Name + " " + Fill(Number.ToString("X2"), 4); }
        }

        private MaskVersion _mask = null;
        [JsonIgnore]
        public MaskVersion Mask
        {
            get { return _mask; }
            set { _mask = value; Changed("Mask"); }
        }

        [JsonIgnore]
        public string _maskId;
        public string MaskId
        {
            get { return _mask.Id; }
            set { _maskId = value; }
        }


        public ObservableCollection<AppVersion> Versions { get; set; } = new ObservableCollection<AppVersion>();

        public event PropertyChangedEventHandler PropertyChanged;

        private void Changed(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private string Fill(string input, int length)
        {
            for (int i = input.Length; i < length; i++)
                input = "0" + input;
            return input;
        }
    }
}
