using Kaenx.DataContext.Import.Dynamic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Kaenx.Creator.Viewer.Selectors
{
    public class ChannelSelector : DataTemplateSelector
    {
        public DataTemplate Channel { get; set; }
        public DataTemplate Independent { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            switch (item)
            {
                case ChannelBlock cb:
                    return Channel;

                case ChannelIndependentBlock cib:
                    return Independent;
            }

            return Independent;
        }
    }
}