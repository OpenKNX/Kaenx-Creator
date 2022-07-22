using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text;

namespace Kaenx.Creator.Models
{
    public class Translation : INotifyPropertyChanged
    {
        public Translation() {}
        public Translation(Language lang, string text) {
            Language = lang;
            Text = text;
        }

        private Language _lang = null;
        public Language Language
        {
            get { return _lang; }
            set { _lang = value; Changed("Language"); }
        }

        private string _text = "Übersetzung";
        public string Text
        {
            get { return _text; }
            set { _text = value; Changed("Text"); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void Changed(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
