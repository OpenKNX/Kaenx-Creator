using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

// Die Elementvorlage "Benutzersteuerelement" wird unter https://go.microsoft.com/fwlink/?LinkId=234236 dokumentiert.

namespace Kaenx.Creator.Viewer.Controls
{
    public sealed partial class Slider : UserControl
    {
        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register("Value", typeof(double), typeof(Slider), new PropertyMetadata(new PropertyChangedCallback(PropertyChanged)));
        public static readonly DependencyProperty ValueOkProperty = DependencyProperty.Register("ValueOk", typeof(double), typeof(Slider), new PropertyMetadata(null));
        public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register("Maximum", typeof(double), typeof(Slider), new PropertyMetadata());
        public static readonly DependencyProperty MinimumProperty = DependencyProperty.Register("Minimum", typeof(double), typeof(Slider), new PropertyMetadata());
        public static readonly DependencyProperty IntervalProperty = DependencyProperty.Register("Interval", typeof(double), typeof(Slider), new PropertyMetadata());


        private double _value;
        public double Value { 
            get { return (double)GetValue(ValueProperty); }
            set {
                SetValue(ValueProperty, value); 
            }
        }

        public double ValueOk { 
            get { return (double)GetValue(ValueOkProperty); }
            set { SetValue(ValueOkProperty, value); }
        }

        public double Maximum
        {
            get { return (double)GetValue(MaximumProperty); }
            set { SetValue(MaximumProperty, value); }
        }

        public double Minimum
        {
            get { return (double)GetValue(MinimumProperty); }
            set { SetValue(MinimumProperty, value); }
        }
        
        public double Interval
        {
            get { return (double)GetValue(IntervalProperty); }
            set { SetValue(IntervalProperty, value); }
        }
        
        public string Tooltip { get { return Minimum + " - " + Maximum; } }

        private static void PropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            (sender as Slider)?.OnValueChanged((double)e.NewValue);
        }

        private async void OnValueChanged(double value)
        {
            _value = value;
            await System.Threading.Tasks.Task.Delay(1500);
            if(_value == value)
            {
                SetValue(ValueOkProperty, _value);
            }
        }

        public Slider()
        {
            this.InitializeComponent();
        }
    }
}