using System;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;

namespace Kaenx.Creator.Models {
    public class CompletionData : ICompletionData
	{
		public CompletionData(string text, bool prop = true)
		{
			this.Text = text;
            this.IsProperty = prop;
		}
		
		public System.Windows.Media.ImageSource Image {
			get { return null; }
		}
		
		public string Text { get; private set; }
        public bool IsProperty {get;set;}
		
		// Use this property if you want to show a fancy UIElement in the drop down list.
		public object Content {
			get { return this.Text; }
		}
		
		public object Description {
			get { return "Description for " + this.Text; }
		}
		
		public double Priority { get { return 0; } }
		
		public void Complete(TextArea textArea, ISegment completionSegment, EventArgs insertionRequestEventArgs)
		{
            string replacing = this.Text;
            if(this.IsProperty) replacing += "=\"";
			textArea.Document.Replace(completionSegment, replacing);
		}
	}
}