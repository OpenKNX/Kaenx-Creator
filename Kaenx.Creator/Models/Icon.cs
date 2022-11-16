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
    public class Icon
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


        private bool _dataChanged = false;
        private byte[] _data;
        public byte[] Data
        {
            get { return _data; }
            set { _dataChanged = true; _data = value; }
        }

        BitmapImage image;
        MemoryStream ms = null;

        public ImageSource Source
        {
            get {
                if(image == null || _dataChanged)
                {
                    if(ms != null) ms.Dispose();
                    ms = new MemoryStream(Data);
                    image = new BitmapImage();
                    image.BeginInit();
                    image.StreamSource = ms;
                    image.EndInit();
                    _dataChanged = false;
                }
                
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