using ICSharpCode.AvalonEdit.CodeCompletion;
using Kaenx.Creator.Models;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Kaenx.Creator.Controls
{
    public partial class LoadProcedures : UserControl
    {
        public static readonly DependencyProperty VersionProperty = DependencyProperty.Register("Version", typeof(AppVersion), typeof(LoadProcedures), new PropertyMetadata());
        public static readonly DependencyProperty MaskProperty = DependencyProperty.Register("Mask", typeof(MaskVersion), typeof(LoadProcedures), new PropertyMetadata());
        public AppVersion Version {
            get { return (AppVersion)GetValue(VersionProperty); }
            set { SetValue(VersionProperty, value); }
        }
        public IVersionBase Mask {
            get { return (IVersionBase)GetValue(MaskProperty); }
            set { SetValue(MaskProperty, value); }
        }
        
        public LoadProcedures()
		{
			InitializeComponent();
            editor.TextArea.TextEntered += EditorEntered;
            editor.TextArea.TextEntering += EditorEntering;
            this.DataContext = this;
		}

        CompletionWindow completionWindow;
        private void EditorEntered(object sender, TextCompositionEventArgs e)
        {
            if (e.Text == " ") {
                // Open code completion after the user has pressed dot:
                completionWindow = new CompletionWindow(editor.TextArea);
                IList<ICompletionData> data = completionWindow.CompletionList.CompletionData;
                data.Add(new Models.CompletionData("LsmIdx"));
                data.Add(new Models.CompletionData("ObjIdx"));
                data.Add(new Models.CompletionData("Address"));
                data.Add(new Models.CompletionData("Size"));
                completionWindow.Show();
                completionWindow.Closed += delegate {
                    completionWindow = null;
                };
            }
            if(e.Text == "<") {
                completionWindow = new CompletionWindow(editor.TextArea);
                IList<ICompletionData> data = completionWindow.CompletionList.CompletionData;
                data.Add(new Models.CompletionData("LdCtrlConnect", false));
                data.Add(new Models.CompletionData("LcCtrlDisconnect", false));
                data.Add(new Models.CompletionData("LcCtrlWriteMemory", false));
                completionWindow.Show();
                completionWindow.Closed += delegate {
                    completionWindow = null;
                };
            }
        }

        private void EditorEntering(object sender, TextCompositionEventArgs e)
		{
            if(e.Text == "/") {
                //editor.Text += "/>";
                editor.Document.Insert(editor.SelectionStart, "/>");
                if(completionWindow != null) completionWindow.Close();
                e.Handled = true;
                return;
            } 
            if(e.Text == "<") {
                int selected = editor.SelectionStart;
                editor.Document.Insert(editor.SelectionStart, "< />");
                editor.SelectionStart = selected + 1;
                e.Handled = true;
                EditorEntered(sender, e);
                return;
            }
			if (e.Text.Length > 0 && completionWindow != null) {
                
				if (!char.IsLetter(e.Text[0])) {
					// Whenever a non-letter is typed while the completion window is open,
					// insert the currently selected element.
					completionWindow.CompletionList.RequestInsertion(e);
				}
			}
			// do not set e.Handled=true - we still want to insert the character that was typed
		}
    }
}