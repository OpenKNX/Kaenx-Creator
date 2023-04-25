using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.Windows.Data;
using System.Globalization;
using System.ComponentModel;
using Newtonsoft.Json;

namespace Kaenx.Creator.Models
{
    public class OpenKnxModule : INotifyPropertyChanged
    {
        private int _uid = -1;
        public int UId
        {
            get { return _uid; }
            set { _uid = value; Changed("UId"); }
        }

        private string _name = "";
        public string Name {
            get { return _name; }
            set { _name = value; Changed("Name"); }
        }

        private string _prefix = "";
        public string Prefix {
            get { return _prefix; }
            set { _prefix = value; Changed("Prefix"); }
        }

        private string _url = "";
        public string Url {
            get { return _url; }
            set { _url = value; Changed("Url"); }
        }

        private string _branch = "";
        public string Branch {
            get { return _branch; }
            set { _branch = value; Changed("Branch"); }
        }

        private string _commit = "";
        public string Commit {
            get { return _commit; }
            set { _commit = value; Changed("Commit"); }
        }

        private string _state = "";
        [JsonIgnore]
        public string State {
            get { return _state; }
            set { _state = value; Changed("State"); }
        }

        private bool _hasShare = false;
        public bool HasShare {
            get { return _hasShare; }
            set { _hasShare = value; Changed("HasShare"); }
        }

        private bool _hasPart = false;
        public bool HasPart {
            get { return _hasPart; }
            set { _hasPart = value; Changed("HasPart"); }
        }

        private bool _hasTemplate = false;
        public bool HasTemplate {
            get { return _hasTemplate; }
            set { _hasTemplate = value; Changed("HasTemplate"); }
        }


        public List<OpenKnxNum> NumChannels { get; set; } = new List<OpenKnxNum>();


        public void AddState(string line)
        {
            State += line + "\r\n";
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void Changed(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

    }
}