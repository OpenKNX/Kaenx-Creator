using System;
using System.Collections.Generic;
using System.Text;

namespace Kaenx.Creator.Models
{
    public class ExportItem
    {
        public bool Selected { get; set; }
        public Hardware Hardware { get; set; }
        public Application App { get; set; }
        public AppVersion Version { get; set; }
        public Device Device { get; set; }
    }
}
