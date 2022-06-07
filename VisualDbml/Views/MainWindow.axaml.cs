using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using AvaloniaEdit;
using AvaloniaEdit.CodeCompletion;
using AvaloniaEdit.Editing;
using AvaloniaEdit.TextMate;
using DynamicData;
using VisualDbml.Model.TextEditor;

namespace VisualDbml.Views;

public class MainWindow : Window
{
	public MainWindow()
	{
		InitializeComponent();
#if DEBUG
		this.AttachDevTools();
#endif

		_editor = this.FindControl<TextEditor>("Editor");
		_editor.TextArea.TextEntering += OnTextEntering;
		_editor.TextArea.TextEntered += OnTextEntered;
		_editor.KeyDown += OnEditorKeyDown;

		var registryOptions = new DbmlRegistryOptions();
		var textMate = _editor.InstallTextMate(registryOptions);
		textMate.SetGrammar("source.dbml");
	}

	private IEnumerable<ICompletionData> GenerateCodeCompletionData()
	{
		var words = WordPattern
			.Split(_editor.Document.Text)
			.Where(s => !s.All(char.IsDigit) && s.Length > 1);

		return
			KeywordList.Concat(
					words
				)
				.Distinct()
				.Select(str => new CompletionData(str));
	}

	private void InitializeComponent()
	{
		AvaloniaXamlLoader.Load(this);
	}

	private void OnEditorKeyDown(object? sender, KeyEventArgs e)
	{
		if (e.Key == Key.Space && e.KeyModifiers.HasFlag(KeyModifiers.Control))
		{
			OpenCompletionWindow("");
			e.Handled = true;
		}
	}

	private void OnTextEntered(object? sender, TextInputEventArgs e)
	{
		//if( !string.IsNullOrWhiteSpace(e.Text))
		//	OpenCompletionWindow(e.Text);
	}

	private void OnTextEntering(object? sender, TextInputEventArgs e)
	{
		if (e.Text == null)
			return;

		if (_completionWindow != null && e.Text.Length > 0)
		{
			if (!char.IsLetterOrDigit(e.Text[0]))
			{
				// Whenever a non-letter is typed while the completion window is open,
				// insert the currently selected element.
				_completionWindow.CompletionList.RequestInsertion(e);
			}
		}
	}

	private void OpenCompletionWindow(string text)
	{
		if (_completionWindow != null)
			return;

		_completionWindow = new DebugCompletionWindow(_editor.TextArea);
		_completionWindow.Closed += delegate { _completionWindow = null; };

		IList<ICompletionData> data = _completionWindow.CompletionList.CompletionData;

		data.AddRange(GenerateCodeCompletionData());

		_completionWindow.CloseAutomatically = true;
		_completionWindow.CloseWhenCaretAtBeginning = true;
		_completionWindow.Show();
		_completionWindow.StartOffset -= text.Length;
	}

	private readonly TextEditor _editor;
	private CompletionWindow? _completionWindow;
	private static readonly string[] KeywordList = { "table" };
	private static readonly Regex WordPattern = new("\\W+");
}

internal class DebugCompletionWindow : CompletionWindow
{
	public DebugCompletionWindow(TextArea textArea)
		: base(textArea)
	{
	}

	protected override Size ArrangeOverride(Size finalSize)
	{
		var result = base.ArrangeOverride(finalSize);
		Debug.WriteLine($"ArrangeOverride: {finalSize} -> {result}");
		return result;
	}

	protected override Size MeasureCore(Size availableSize)
	{
		var result = base.MeasureCore(availableSize);
		Debug.WriteLine($"MeasureCore: {availableSize} -> {result}");
		return result;
	}

	protected override Size MeasureOverride(Size availableSize)
	{
		var result = base.MeasureOverride(availableSize);
		Debug.WriteLine($"MeasureOverride: {availableSize} -> {result}");
		return result;
	}

	protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
	{
		base.OnAttachedToVisualTree(e);
		Debug.WriteLine("Attached");
	}
}