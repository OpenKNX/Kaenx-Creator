using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.ObjectModel;

namespace Kaenx.Creator.Models
{
    public class Application
    {
        public string Name { get; set; } = "Dummy";
        public int Number { get; set; } = 1;

        public ObservableCollection<AppVersion> Versions { get; set; } = new ObservableCollection<AppVersion>();
    }
}
