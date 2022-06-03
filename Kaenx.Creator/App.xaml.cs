using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Kaenx.Creator
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

            this.Dispatcher.UnhandledException += OnDispatcherUnhandledException;

            void OnDispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e) {
                if (e.Exception is System.Windows.Markup.XamlParseException) return;

                string errorMessage = string.Format("An unhandled exception occurred: {0}", e.Exception.Message);
                errorMessage += "\r\n\r\n" + e.Exception.StackTrace;
                MessageBox.Show(errorMessage, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                MessageBox.Show("Please save your project and close the application.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                e.Handled = true;
            }
        }
    }
}
