using Kaenx.Creator.Models.Dynamic;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace Kaenx.Creator.Selectors
{
    public class DynSelector : DataTemplateSelector
    {
        public DataTemplate DChannel { get; set; }
        public DataTemplate DBlock { get; set; }
        public DataTemplate DPara { get; set; }
        public DataTemplate DChoose { get; set; }
        public DataTemplate DWhen { get; set; }
        public DataTemplate DMain { get; set; }
        public DataTemplate DIndependent { get; set; }


        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            switch (item)
            {
                case DynChannel dc:
                    return DChannel;

                case DynParaBlock dpb:
                    return DBlock;

                case DynParameter dp:
                    return DPara;

                case DynChoose dco:
                    return DChoose;

                case DynWhen dw:
                    return DWhen;

                case DynamicMain dm:
                    return DMain;

                case DynChannelIndependet dic:
                    return DIndependent;
            }

            return null;
        }
    }
}
