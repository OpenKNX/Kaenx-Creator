using Kaenx.Creator.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace Kaenx.Creator.Selectors
{
    public class MemorySelector : DataTemplateSelector
    {
        public DataTemplate Normal { get; set; }
        public DataTemplate GATable { get; set; }
        public DataTemplate COTable { get; set; }
        public DataTemplate ASTable { get; set; }
        public DataTemplate Module { get; set; }
        public DataTemplate Bcu1Data { get; set; }


        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            MemoryByte mem = (MemoryByte)item;
            if(item == null || mem == null) return null;

            switch (mem.Usage)
            {
                case MemoryByteUsage.GroupAddress:
                    return GATable;

                case MemoryByteUsage.Association:
                    return ASTable;

                case MemoryByteUsage.Coms:
                    return COTable;

                case MemoryByteUsage.Module:
                    return Module;

                case MemoryByteUsage.Bcu1Data:
                    return Bcu1Data;
            }

            return Normal;
        }
    }
}
