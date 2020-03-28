using System;
using System.Collections.Generic;
using System.Text;

namespace Kaenx.Creator.Models
{
    public class AppVersion
    {
        public string VersionText { get { return "V " + VersionNumber; } }
        public int VersionNumber { get; set; } = 0;
    }
}
