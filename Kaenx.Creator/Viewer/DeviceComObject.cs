using Kaenx.DataContext.Catalog;
using Kaenx.DataContext.Import.Dynamic;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kaenx.Creator.Viewer
{
    public class DeviceComObject : INotifyPropertyChanged
    {
        public DeviceComObject() 
        {
            //nothing to do
        }

        public DeviceComObject(AppComObject comObj)
        {
            Id = comObj.Id;
            Number = comObj.Number;
            Name = comObj.Text;
            Function = comObj.FunctionText;

            Flag_Read = comObj.Flag_Read;
            Flag_Write = comObj.Flag_Write;
            Flag_Update = comObj.Flag_Update;
            Flag_Transmit = comObj.Flag_Transmit;
            Flag_Communication = comObj.Flag_Communicate;
            Flag_ReadOnInit = comObj.Flag_ReadOnInit;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        //public DataPointSubType DataPointSubType { get; set; }
        public bool IsSelected { get; set; } = false;
        public bool IsEnabled { get; set; } = true;
        public bool IsOk { get; set; } = true;

        private string _name;

        public long Id { get; set; }
        public int BindedId { get; set; }
        public int Number { get; set; }
        public string Name { get { return _name; } set { _name = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Name")); } }
        public string Function { get; set; }
        


        public bool Flag_Read { get; set; }
        public bool Flag_Write { get; set; }
        public bool Flag_Update { get; set; }
        public bool Flag_Transmit { get; set; }
        public bool Flag_Communication { get; set; }
        public bool Flag_ReadOnInit { get; set; }
    }
}