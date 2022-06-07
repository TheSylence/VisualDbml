using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using AvaloniaEdit.Document;
using DynamicData.Binding;
using ReactiveUI;
using VisualDbml.Model.Dbml;
using VisualDbml.Model.Graph;

namespace VisualDbml.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
	public MainWindowViewModel()
	{
		NewCommand = ReactiveCommand.CreateFromTask(New);
		OpenCommand = ReactiveCommand.CreateFromTask(Open);
		QuitCommand = ReactiveCommand.CreateFromTask(Quit);
		SaveCommand = ReactiveCommand.CreateFromTask(Save);
		SaveAsCommand = ReactiveCommand.CreateFromTask(SaveAs);

		var modifiedChanged = this.WhenPropertyChanged(vm => vm.IsModified);
		var fileNameChanged = Observable.FromEventPattern(Document, nameof(Document.FileNameChanged));

		modifiedChanged.Merge<object>(fileNameChanged).Subscribe(_ =>
		{
			WindowTitle = $"{Document.FileName}{(string?)(IsModified ? "*" : "")} - VisualDBML";
		});

		var documentChanged = Observable.FromEventPattern(Document, nameof(Document.Changed));
		documentChanged
			.Select(evt => (evt.Sender as TextDocument)?.Text)
			.Where(str => !string.IsNullOrEmpty(str))
			.Throttle(TimeSpan.FromMilliseconds(500))
			.Subscribe(DocumentOnTextChanged!);

		New().ContinueWith(_ => { });
	}

	public TextDocument Document { get; } = new();

	public string Graph
	{
		get => _graph;
		set => this.RaiseAndSetIfChanged(ref _graph, value);
	}

	public bool IsModified
	{
		get => _isModified;
		set => this.RaiseAndSetIfChanged(ref _isModified, value);
	}

	public ICommand NewCommand { get; }
	public ICommand OpenCommand { get; }
	public ICommand QuitCommand { get; }
	public ICommand SaveAsCommand { get; }
	public ICommand SaveCommand { get; }

	public string WindowTitle
	{
		get => _windowTitle;
		set => this.RaiseAndSetIfChanged(ref _windowTitle, value);
	}

	private Task<bool> CheckForUnsavedChanges() => Task.FromResult(true);

	private void DocumentOnTextChanged(string text)
	{
		if (File.Exists(Graph))
			File.Delete(Graph);

		try
		{
			var model = _dbmlParser.Parse(text);
			if (model == null)
				return;

			Graph = _graphGenerator.CreateGraph(model);
		}
		catch
		{
			// ignored
		}
	}

	private async Task New()
	{
		if (!await CheckForUnsavedChanges())
			return;

		Document.Text = "";
		Document.FileName = "Untitled";
		IsModified = false;
	}

	private async Task Open()
	{
		if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
			return;

		if (!await CheckForUnsavedChanges())
			return;

		var dlg = new OpenFileDialog
		{
			Filters = new List<FileDialogFilter>
			{
				new()
				{
					Name = "Database Markup files (.dbml)",
					Extensions = new List<string>
					{
						"dbml"
					}
				}
			}
		};

		var fileNames = await dlg.ShowAsync(desktop.MainWindow);
		var fileName = fileNames?.FirstOrDefault();
		if (string.IsNullOrEmpty(fileName))
			return;

		await OpenFile(fileName);
	}

	private async Task OpenFile(string fileName)
	{
		var content = await File.ReadAllTextAsync(fileName);
		Document.Text = content;
		Document.FileName = fileName;
		IsModified = false;
	}

	private async Task Quit()
	{
		if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
			return;

		if (!await CheckForUnsavedChanges())
			return;

		desktop.Shutdown();
	}

	private async Task Save()
	{
		if (!File.Exists(Document.FileName))
			await SaveAs();
		else
			await SaveFile(Document.FileName);
	}

	private async Task SaveAs()
	{
		if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
			return;

		var dlg = new SaveFileDialog
		{
			Filters = new List<FileDialogFilter>
			{
				new()
				{
					Name = "Database Markup files (.dbml)",
					Extensions = new List<string>
					{
						"dbml"
					}
				}
			}
		};

		var fileName = await dlg.ShowAsync(desktop.MainWindow);
		if (string.IsNullOrEmpty(fileName))
			return;

		await SaveFile(fileName);
	}

	private async Task SaveFile(string fileName)
	{
		var content = Document.Text;
		await File.WriteAllTextAsync(fileName, content);

		Document.FileName = fileName;
		IsModified = false;
	}

	private readonly DbmlParser _dbmlParser = new();
	private readonly GraphGenerator _graphGenerator = new();

	private string _windowTitle = "";
	private bool _isModified;
	private string _graph;
}