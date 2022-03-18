using Kaenx.Creator.Models;
using Kaenx.Creator.Models.Dynamic;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Collections.ObjectModel;
using System.Text;

namespace Kaenx.Creator.Models
{
    public class Module : INotifyPropertyChanged, IVersionBase
    {
        private int _uid = -1;
        public int UId
        {
            get { return _uid; }
            set { _uid = value; Changed("UId"); }
        }
        
        private int _id = -1;
        public int Id
        {
            get { return _id; }
            set { _id = value; Changed("Id"); }
        }

        
        private string _name = "dummy";
        public string Name
        {
            get { return _name; }
            set { _name = value; Changed("Name"); }
        }

        private bool _isAutoPR = true;
        public bool IsParameterRefAuto
        {
            get { return _isAutoPR; }
            set { _isAutoPR = value; Changed("IsParameterRefAuto"); }
        }
        
        private bool _isAutoCR= true;
        public bool IsComObjectRefAuto
        {
            get { return _isAutoCR; }
            set { _isAutoCR = value; Changed("IsComObjectRefAuto"); }
        }
    
        public ObservableCollection<Parameter> Parameters { get; set; } = new ObservableCollection<Parameter>();
        public ObservableCollection<ParameterRef> ParameterRefs { get; set; } = new ObservableCollection<ParameterRef>();
        public ObservableCollection<ComObject> ComObjects { get; set; } = new ObservableCollection<ComObject>();
        public ObservableCollection<ComObjectRef> ComObjectRefs { get; set; } = new ObservableCollection<ComObjectRef>();
        public ObservableCollection<Union> Unions { get; set; } = new ObservableCollection<Union>();
        public ObservableCollection<Argument> Arguments { get; set; } = new ObservableCollection<Argument>();
        public List<DynamicMain> Dynamics { get; set; } = new List<DynamicMain>();


        public event PropertyChangedEventHandler PropertyChanged;
        private void Changed(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
