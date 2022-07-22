using System;
using System.Collections.Generic;
using System.Text;

namespace Kaenx.Creator.Models
{
    public class EtsVersion
    {
        public int Number { get; set; }
        public string DisplayName { get; set; }
        public string FolderPath { get; set; }
        public bool IsEnabled { get; set; }

        public EtsVersion(int number, string name, string folder) {
            Number = number;
            DisplayName = name;
            FolderPath = folder;
        }
    }
}
