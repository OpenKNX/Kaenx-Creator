using System;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using System.Windows;

namespace Kaenx.Creator.Controls
{
    public partial class ProdVParameterDialog : Window
    {
        public ProdVParameterDialog(string question, string title, List<Viewer.ModuleModel> mods)
		{
			InitializeComponent();
            Question.Text = question;
            this.Title = title;
            mods.Insert(0, new Viewer.ModuleModel() { Id = Properties.Messages.prodv_no_mod, Start = 0 });
            this.modList.ItemsSource = mods;
            this.modList.SelectedIndex = 0;
		}

        private void btnDialogOk_Click(object sender, RoutedEventArgs e)
		{
			this.DialogResult = true;
		}

        public long Answer { get { return int.Parse(AnswerBox.Text) + (long)(modList.SelectedValue ?? 0); } }
    }
}