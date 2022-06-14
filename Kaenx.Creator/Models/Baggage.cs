using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Kaenx.Creator.Models
{
    public class Baggage
    {
        
        private int _uid = -1;
        public int UId
        {
            get { return _uid; }
            set { _uid = value; Changed("UId"); }
        }

        private string _name = "dummy";
        public string Name
        {
            get { return _name; }
            set { _name = value; Changed("Name"); }
        }

        public string Extension { get; set; }
        public string TargetPath { get; set; } = "";
        public DateTime TimeStamp { get; set; } = DateTime.Now;

        public byte[] Data { get; set; }

        public ImageSource Source
        {
            get {
                BitmapImage image = new BitmapImage();
                MemoryStream ms = new MemoryStream(Data);
                image.BeginInit();
                image.StreamSource = ms;
                image.EndInit();
                return image;
            }
        }


        public event PropertyChangedEventHandler PropertyChanged;
        private void Changed(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}