using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.ObjectModel;

namespace Kaenx.Creator.Models
{
    public class ParameterTypeEnum
    {
        public string Name {get;set;}
        public int Value {get;set;}
        public ObservableCollection<Translation> Text {get;set;} = new ObservableCollection<Translation>();
    }
}