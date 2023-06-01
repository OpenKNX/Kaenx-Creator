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
    public class Property : IParameterSavePath, INotifyPropertyChanged
    {
        public int ObjectType { get; set; } = 0;
        public int PropertyId { get; set; } = 1;
        public int Offset { get; set; }
        public int OffsetBit { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        private void Changed(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
