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
    public class ParameterTypeSelector : DataTemplateSelector
    {
        public DataTemplate NotFound { get; set; }
        public DataTemplate Number { get; set; }
        public DataTemplate Text { get; set; }
        public DataTemplate TextRead { get; set; }
        public DataTemplate Enums { get; set; }
        public DataTemplate EnumsTwo { get; set; }
        public DataTemplate CheckBox { get; set; }
        public DataTemplate Color { get; set; }
        public DataTemplate Seperator { get; set; }
        public DataTemplate SeperatorBox { get; set; }
        public DataTemplate Time { get; set; }
        public DataTemplate Table { get; set; }
        public DataTemplate Slider { get; set; }
        public DataTemplate Picture { get; set; }
        public DataTemplate None { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            switch(item)
            {
                case ParamEnum pen:
                    return Enums;

                case ParamEnumTwo pent:
                    return EnumsTwo;

                case ParamNumber pnu:
                    return Number;

                case ParamTextRead pter:
                    return TextRead;

                case ParamText pte:
                    return Text;

                case ParamCheckBox pch:
                    return CheckBox;

                case ParamColor pco:
                    return Color;

                case ParamSeperator pse:
                    return Seperator;

                case ParamSeperatorBox psex:
                    return SeperatorBox;

                case ParamTime pti:
                    return Time;

                case ParameterTable ptable:
                    return NotFound;

                case ParamSlider psl:
                    return Slider;

                case ParamNone pnon:
                    return None;
            }

            return NotFound;
        }
    }
}