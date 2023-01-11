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
        public DataTemplate DMainM { get; set; }
        public DataTemplate DIndependent { get; set; }
        public DataTemplate DModule { get; set; }
        public DataTemplate DCom { get; set; }
        public DataTemplate DSeparator { get; set; }
        public DataTemplate DAssign { get; set; }
        public DataTemplate DRepeat { get; set; }
        public DataTemplate DButton { get; set; }


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

                case IDynChoose dco:
                    return DChoose;

                case IDynWhen dw:
                    return DWhen;

                case DynamicMain dm:
                    return DMain;

                case DynamicModule dmo:
                    return DMainM;

                case DynChannelIndependent dic:
                    return DIndependent;

                case DynComObject dco:
                    return DCom;

                case DynModule dmo:
                    return DModule;

                case DynSeparator ds:
                    return DSeparator;

                case DynAssign da:
                    return DAssign;

                case DynRepeat dr:
                    return DRepeat;

                case DynButton db:
                    return DButton;
            }

            return null;
        }
    }
}
