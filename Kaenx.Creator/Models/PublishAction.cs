using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace Kaenx.Creator.Models
{
    public class PublishAction : INotifyPropertyChanged
    {
        private PublishState _state = PublishState.Waiting;
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

        public event PropertyChangedEventHandler PropertyChanged;
        private void Changed(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    public enum PublishState
    {
        Waiting,
        Success,
        Fail,
        Warning
    }
}
