using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace Kaenx.Creator.Models
{
    public class PublishAction : INotifyPropertyChanged
    {
        private PublishState _state = PublishState.Info;
        public PublishState State
        {
            get { return _state; }
            set { _state = value; Changed("State"); }
        }


        private string _text = "";
        public string Text
        {
            get { return _text; }
            set { _text = value; Changed("Text"); }
        }

        public object Item { get; set; }
        public object Module { get; set; }
        public bool CanGoToItem { get { return Item != null; } }

        public event PropertyChangedEventHandler PropertyChanged;
        private void Changed(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    public enum PublishState
    {
        Info,
        Success,
        Fail,
        Warning
    }
}
