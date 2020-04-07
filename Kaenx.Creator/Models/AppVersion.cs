using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace Kaenx.Creator.Models
{
    public class AppVersion
    {

        public ObservableCollection<ParameterType> ParameterTypes { get; set; } = new ObservableCollection<ParameterType>();
        public ObservableCollection<Parameter> Parameters { get; set; } = new ObservableCollection<Parameter>();


        public string VersionText { 
            get {
                int main = (int)Math.Floor((double)Number / 16);
                int sub = Number - (main * 16);
                return "V " + main + "." + sub; 
            } 
        }

        public int Number { get; set; } = 16;
    }
}
