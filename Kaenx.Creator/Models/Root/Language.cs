using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text;

namespace Kaenx.Creator.Models
{
    public class Language : INotifyPropertyChanged
    {

        public Language() {}
        public Language(string text, string culture) {
            Text = text;
            CultureCode = culture;
        }

        private string _cultureCode = "de-de";
        public string CultureCode
        {
            get { return _cultureCode; }
            set { _cultureCode = value; Changed("CultureCode"); }
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
