using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using Kaenx.Creator.Models;
using Kaenx.Creator.Models.Dynamic;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows;

namespace Kaenx.Creator.Classes
{
    public static class AutoHelper
    {
        public static byte[] GetFileBytes(string file)
        {
            byte[] data;
            BitmapImage image = new BitmapImage(new Uri(file));
            BitmapEncoder encoder;

            switch(Path.GetExtension(file).ToLower())
            {
                case ".png":
                    encoder = new PngBitmapEncoder();
                    break;

                case ".jpg":
                case ".jpeg":
                    encoder = new JpegBitmapEncoder();
                    break;

                default:
                    throw new Exception("Dataityp " + Path.GetExtension(file).ToLower() + " wird nicht unterstützt");
            }
            
            encoder.Frames.Add(BitmapFrame.Create(image));
            using (MemoryStream ms = new MemoryStream())
            {
                encoder.Save(ms);
                data = new byte[ms.Length];
                ms.ToArray().CopyTo(data, 0);
            }
            image = null;
            encoder.Frames.RemoveAt(0);
            encoder = null;
            
            return data;
        }
    }
}
