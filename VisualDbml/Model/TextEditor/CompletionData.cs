using System;
using Avalonia.Media.Imaging;
using AvaloniaEdit.CodeCompletion;
using AvaloniaEdit.Document;
using AvaloniaEdit.Editing;

namespace VisualDbml.Model.TextEditor;

internal class CompletionData : ICompletionData
{
	public CompletionData(string text)
	{
		Text = text;
	}

	public object Content => Text;
	public object Description => null;
	public IBitmap Image => null;
	public double Priority => 1.0;
	public string Text { get; }

	public void Complete(
		TextArea textArea, 
		ISegment completionSegment,
		EventArgs insertionRequestEventArgs)
	{
		textArea.Document.Replace(completionSegment, Text);
	}
}