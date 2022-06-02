using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Windows.Data;
using System.Globalization;
using System.ComponentModel;
using System.Windows.Media;

namespace Kaenx.Creator.Models
{
    public class MemorySection : INotifyPropertyChanged
    {   
        public int Address;

        public string Name
        {
            get { return $"0x{Address:X4}"; }
        }

        public MemorySection(int address)
        {
            Address = address;
        }

        public MemorySection(int address, int usedBits = 0)
        {
            Address = address;
        }

        
        public ObservableCollection<MemoryByte> Bytes {get;set;} = new ObservableCollection<MemoryByte>();

        private List<SolidColorBrush> fillColor;
        public List<SolidColorBrush> FillColor
        {
            get{
                CalculateFillColors();
                return fillColor;
            }
        }


        private void CalculateFillColors()
        {
            if(fillColor != null) return;
            fillColor = new List<SolidColorBrush>();
            foreach(MemoryByte mbyte in Bytes)
            {

                switch(mbyte.Usage)
                {
                    case MemoryByteUsage.Used:
                        fillColor.Add(new SolidColorBrush(Colors.Gray));
                        continue;

                    case MemoryByteUsage.GroupAddress:
                        fillColor.Add(new SolidColorBrush(Colors.Violet));
                        continue;

                    case MemoryByteUsage.Association:
                        fillColor.Add(new SolidColorBrush(Colors.Brown));
                        continue;

                    case MemoryByteUsage.Coms:
                        fillColor.Add(new SolidColorBrush(Colors.Chocolate));
                        continue;
                }
                
                if(mbyte.UnionObject != null)
                {
                    fillColor.Add(new SolidColorBrush(Colors.Blue));
                    continue;
                }

                (int size, int offset) usage = mbyte.GetFreeBits();
                switch(usage.size)
                {
                    case 0:
                        fillColor.Add(new SolidColorBrush(Colors.Red));
                        break;

                    case 8:
                        fillColor.Add(new SolidColorBrush(Colors.Green));
                        break;

                    default:
                        fillColor.Add(new SolidColorBrush(Colors.Orange));
                        break;
                }
            }

            int toadd = 16-fillColor.Count;
            for(int i = 0; i<toadd; i++)
                fillColor.Add(new SolidColorBrush(Colors.Gray));
        }


        public event PropertyChangedEventHandler PropertyChanged;
        private void Changed(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}