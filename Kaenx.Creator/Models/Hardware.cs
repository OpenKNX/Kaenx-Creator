using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text;

namespace Kaenx.Creator.Models
{
    public class Hardware:INotifyPropertyChanged
    {
        public string Name { get; set; } = "Hardware Name";
        public string SerialNumber { get; set; } = "1";
        [JsonIgnore]
        public Device DeviceObject { get; set; }
        public string Device
        {
            get { return DeviceObject?.Name; }
            set { _device = value; }
        }
        public int Version { get; set; } = 1;
        public int BusCurrent { get; set; } = 10;
        public bool HasIndividualAddress { get; set; } = true;
        private bool _hasApp = true;
        public bool HasApplicationProgram
        {
            get { return _hasApp; }
            set { _hasApp = value; Changed("HasApplicationProgram"); }
        }
        public bool HasApplicationProgram2 { get; set; } = false;
        public bool IsPowerSupply { get; set; } = false;
        public bool IsCoppler { get; set; } = false;
        public bool IsIpEnabled { get; set; } = false;
        public ObservableCollection<HardwareApp> Apps { get; set; } = new ObservableCollection<HardwareApp>();


        public event PropertyChangedEventHandler PropertyChanged;
        public void Changed(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private string _device = "";

        public string GetDevice()
        {
            return _device;
        }
    }

    public class HardwareApp
    {
        [JsonIgnore]
        public Application AppObject { get; set; }
        [JsonIgnore]
        public AppVersion AppVersionObject { get; set; }
        public string App
        {
            get { return AppObject.Name; }
            set { _app = value; }
        }
        public int AppVersion
        {
            get { return AppVersionObject.Number; }
            set { _appVersion = value; }
        }

        public string DisplayText
        {
            get { return App + " " + AppVersionObject.VersionText; }
        }

        private string _app;
        private int _appVersion;

        public string GetApp()
        {
            return _app;
        }

        public int GetVersion()
        {
            return _appVersion;
        }
    }
}
