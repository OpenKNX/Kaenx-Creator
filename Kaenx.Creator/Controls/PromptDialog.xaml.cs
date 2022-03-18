using System;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using System.Windows;

namespace Kaenx.Creator.Controls
{
    public partial class PromptDialog : Window
    {
        public PromptDialog(string question, string title)
		{
			InitializeComponent();
            Question.Text = question;
            this.Title = title;
		}

        private void btnDialogOk_Click(object sender, RoutedEventArgs e)
		{
			this.DialogResult = true;
		}

        public string Answer { get { return AnswerBox.Text; } }
    }
}