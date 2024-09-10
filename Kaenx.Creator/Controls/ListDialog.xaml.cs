using System;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using System.Windows;

namespace Kaenx.Creator.Controls
{
    public partial class ListDialog : Window
    {
        public ListDialog(string question, string title, List<string> items)
		{
			InitializeComponent();
            Question.Text = question;
            this.Title = title;
            ItemsList.ItemsSource = items;
		}

        private void btnDialogOk_Click(object sender, RoutedEventArgs e)
		{
			this.DialogResult = true;
		}

        public string Answer { get { return ItemsList.SelectedItem.ToString(); } }
    }
}