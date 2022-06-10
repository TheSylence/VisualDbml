using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using VisualDbml.Services;

namespace VisualDbml.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
	public MainWindowViewModel()
	{
		_recentlyUsedFilesService = new RecentlyUsedFilesService();
		
		NewCommand = ReactiveCommand.CreateFromTask(New);
		OpenCommand = ReactiveCommand.CreateFromTask(Open);
		QuitCommand = ReactiveCommand.CreateFromTask(Quit);
		SaveCommand = ReactiveCommand.CreateFromTask(Save);
		SaveAsCommand = ReactiveCommand.CreateFromTask(SaveAs);
		OpenRecentlyUsedFileCommand = ReactiveCommand.CreateFromTask<string>(OpenRecentlyUsedFile);

		var modifiedChanged = this.WhenPropertyChanged(vm => vm.IsModified);
		modifiedChanged.Subscribe(_ =>
		{
			UpdateWindowTitle();
		});

		var documentChanged = Observable.FromEventPattern(this, nameof(DocumentChanged));
		documentChanged
			.Select(evt =>
			{
				var textDocument = evt.Sender as TextDocument;
				return textDocument?.Text;
			})
			.Where(str => !string.IsNullOrEmpty(str))
			.Throttle(TimeSpan.FromMilliseconds(500))
			.Subscribe(DocumentOnTextChanged!);

		PopulateRecentlyUsedFiles();

		New().ContinueWith(_ => { });
	}
	
	

	private Task OpenRecentlyUsedFile(string filePath)
	{
		return OpenFile(filePath);
	}

	private void UpdateWindowTitle()
	{
		WindowTitle = $"{Document.FileName}{(string?)(IsModified ? "*" : "")} - VisualDBML";
	}

	private void PopulateRecentlyUsedFiles()
	{
		RecentlyUsedFiles.Clear();

		foreach (var filePath in _recentlyUsedFilesService.ListRecentlyUsedFiles())
		{
			RecentlyUsedFiles.Add(new RecentlyUsedViewModel(filePath));
		}
	}

	public TextDocument Document
	{
		get => _document;
		set
		{
			_document.Changed -= OnDocumentChanged;
			this.RaiseAndSetIfChanged(ref _document, value);
			_document.Changed += OnDocumentChanged;
		}
	}

	private void OnDocumentChanged(object? sender, DocumentChangeEventArgs e)
	{
		DocumentChanged?.Invoke(sender, e);
	}

	public event EventHandler<DocumentChangeEventArgs>? DocumentChanged;

	public string? Graph
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
	public ICommand OpenRecentlyUsedFileCommand { get; }
	public ICommand QuitCommand { get; }

	public ObservableCollection<RecentlyUsedViewModel> RecentlyUsedFiles { get; } = new();
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

		Document = new TextDocument
		{
			Text = "",
			FileName = "Untitled"
		};
		IsModified = false;
		UpdateWindowTitle();
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

		Document = new TextDocument
		{
			Text = content,
			FileName = fileName
		};
		
		Persist(fileName);
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
		Document.FileName = fileName;
		
		await File.WriteAllTextAsync(fileName, content);
		
		Persist(fileName);
	}

	private void Persist(string fileName)
	{
		IsModified = false;
		UpdateWindowTitle();

		_recentlyUsedFilesService.AddFile(fileName);
		PopulateRecentlyUsedFiles();
		
		DocumentOnTextChanged(Document.Text);
	}

	private readonly DbmlParser _dbmlParser = new();
	private readonly GraphGenerator _graphGenerator = new();
	private string _windowTitle = "";
	private bool _isModified;
	private string? _graph;
	private readonly RecentlyUsedFilesService _recentlyUsedFilesService;
	private TextDocument _document = new();
}