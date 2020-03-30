using System;
using System.Collections.Generic;
using System.Text;

namespace Kaenx.Creator.Models
{
    public class AppVersion
    {
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
