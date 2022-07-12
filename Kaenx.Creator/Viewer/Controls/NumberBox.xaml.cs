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
    public sealed partial class NumberBox : UserControl, INotifyPropertyChanged
    {
        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register("Value", typeof(float), typeof(NumberBox), new PropertyMetadata(null));
        public static readonly DependencyProperty ValueOkProperty = DependencyProperty.Register("ValueOk", typeof(float), typeof(NumberBox), new PropertyMetadata((float)0.0, new PropertyChangedCallback(TextProperty_PropertyChanged)));
        public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register("Maximum", typeof(float), typeof(NumberBox), new PropertyMetadata(null));
        public static readonly DependencyProperty MinimumProperty = DependencyProperty.Register("Minimum", typeof(float), typeof(NumberBox), new PropertyMetadata(null));
        public static readonly DependencyProperty DefaultProperty = DependencyProperty.Register("Default", typeof(float), typeof(NumberBox), new PropertyMetadata(null));
        public static readonly DependencyProperty IncrementProperty = DependencyProperty.Register("Increment", typeof(float), typeof(NumberBox), new PropertyMetadata(null));


        public delegate string PreviewChangedHandler(NumberBox sender, float Value);
        public event PreviewChangedHandler PreviewChanged;
        public event PropertyChangedEventHandler PropertyChanged;

        private static void TextProperty_PropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            obj.SetValue(ValueProperty, e.NewValue);
        }


        private string _errMessage;
        public string ErrMessage
        {
            get { return _errMessage; }
            set { _errMessage = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("ErrMessage")); }
        }

        public float Value { 
            get { return (float)GetValue(ValueProperty); }
            set
            {
                bool error = false;
                string handled = PreviewChanged?.Invoke(this, value);
                if (!string.IsNullOrEmpty(handled))
                {
                    error = true;
                    ErrMessage = handled;
                }

                BtnUp.IsEnabled = value < (float)GetValue(MaximumProperty);
                BtnDown.IsEnabled = value > (float)GetValue(MinimumProperty);

                if (value > (float)GetValue(MaximumProperty))
                {
                    error = true;
                    ErrMessage = "Zahl größer als Maximum von " + (float)GetValue(MaximumProperty);
                }

                if (value < (float)GetValue(MinimumProperty))
                {
                    error = true;
                    ErrMessage = "Zahl kleiner als Minimum  von " + (float)GetValue(MinimumProperty);
                }

                if(Value == value)
                {
                    System.Diagnostics.Debug.WriteLine("Zahl hat sich nicht geändert");
                    return;
                }


                decimal res = (decimal)Math.Round((double)(value - Default), 2);
                decimal mod = res % (decimal)Increment;
                if(res != 0 && mod != 0)
                {
                    error = true;
                    ErrMessage = $"Wert nur in {Increment}er Schritten";
                }


                SetValue(ValueProperty, value);

                if (!error)
                {
                    SetValue(ValueOkProperty, value);
                    VisualStateManager.GoToState(this, "DefaultLayout", false);
                }
                else
                    VisualStateManager.GoToState(this, "NotAcceptedLayout", false);
            }
        }

        public float Default
        {
            get { return (float)GetValue(DefaultProperty); }
            set { SetValue(DefaultProperty, value); }
        }

        public float ValueOk
        {
            get { return (float)GetValue(ValueOkProperty); }
            set { SetValue(ValueProperty, value); SetValue(ValueOkProperty, value); }
        }

        public float Increment
        {
            get { return (float)GetValue(IncrementProperty); }
            set { SetValue(IncrementProperty, value); }
        }

        public float Maximum
        {
            get { return (float)GetValue(MaximumProperty); }
            set
            {
                SetValue(MaximumProperty, value);

                float _val = (float)GetValue(ValueProperty);
                bool error = false;

                BtnUp.IsEnabled = _val < value;
                if (_val > value)
                {
                    error = true;
                    ErrMessage = "Zahl größer als Maximum von " + (float)GetValue(MaximumProperty);
                }

                float min = (float)GetValue(MinimumProperty);
                BtnDown.IsEnabled = _val > min;
                if (_val < min)
                {
                    error = true;
                    ErrMessage = "Zahl kleiner als Minimum  von " + (float)GetValue(MinimumProperty);
                }

                if (!error)
                {
                    SetValue(ValueOkProperty, _val);
                    VisualStateManager.GoToState(this, "DefaultLayout", false);
                }
                else
                    VisualStateManager.GoToState(this, "NotAcceptedLayout", false);
            }
        }

        public float Minimum
        {
            get { return (float)GetValue(MinimumProperty); }
            set
            {
                SetValue(MinimumProperty, value);

                float _val = (float)GetValue(ValueProperty);
                bool error = false;

                float max = (float)GetValue(MaximumProperty);
                BtnUp.IsEnabled = _val < max;
                if (_val > max)
                {
                    error = true;
                    ErrMessage = "Zahl größer als Maximum von " + (float)GetValue(MaximumProperty);
                }

                BtnDown.IsEnabled = _val > value;
                if (_val < value)
                {
                    error = true;
                    ErrMessage = "Zahl kleiner als Minimum  von " + (float)GetValue(MinimumProperty);
                }

                if (!error)
                {
                    SetValue(ValueOkProperty, _val);
                    VisualStateManager.GoToState(this, "DefaultLayout", false);
                }
                else
                    VisualStateManager.GoToState(this, "NotAcceptedLayout", false);
            }
        }
        
        public string Tooltip { get { return $"{Minimum} - {Maximum}; Standard {Default}"; } }



        public NumberBox()
        {
            this.InitializeComponent();
            DataGrid.DataContext = this;
        }

        private void InputBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case System.Windows.Input.Key.D0:
                case System.Windows.Input.Key.D1:
                case System.Windows.Input.Key.D2:
                case System.Windows.Input.Key.D3:
                case System.Windows.Input.Key.D4:
                case System.Windows.Input.Key.D5:
                case System.Windows.Input.Key.D6:
                case System.Windows.Input.Key.D7:
                case System.Windows.Input.Key.D8:
                case System.Windows.Input.Key.D9:
                case System.Windows.Input.Key.NumPad0:
                case System.Windows.Input.Key.NumPad1:
                case System.Windows.Input.Key.NumPad2:
                case System.Windows.Input.Key.NumPad3:
                case System.Windows.Input.Key.NumPad4:
                case System.Windows.Input.Key.NumPad5:
                case System.Windows.Input.Key.NumPad6:
                case System.Windows.Input.Key.NumPad7:
                case System.Windows.Input.Key.NumPad8:
                case System.Windows.Input.Key.NumPad9:
                case System.Windows.Input.Key.Delete:
                case System.Windows.Input.Key.Clear:
                case System.Windows.Input.Key.Back:
                case System.Windows.Input.Key.Left:
                case System.Windows.Input.Key.Right:
                case System.Windows.Input.Key.Decimal:
                    break;
                default:
                    e.Handled = true;
                    break;
            }
        }

        private void GoUp(object sender, RoutedEventArgs e)
        {
            Value = (float)Math.Round((double)(Value + Increment), 2);
        }

        private void GoDown(object sender, RoutedEventArgs e)
        {
            Value -= Increment;
        }

        private void InputBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            float outx;
            bool success = float.TryParse(InputBox.Text, out outx);
            if(success)
            {
                VisualStateManager.GoToState(this, "DefaultLayout", false);    
                Value = (float)Math.Round((double)outx, 2);                
            } else {
                ErrMessage = "Bitte geben Sie eine Zahl ein";
                VisualStateManager.GoToState(this, "NotAcceptedLayout", false);
            }
        }
    }
}