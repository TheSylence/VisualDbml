using System;
using System.IO;
using Avalonia;
using Avalonia.ReactiveUI;
using VisualDbml.Model;

namespace VisualDbml;

class Program
{
	// Initialization code. Don't use any Avalonia, third-party APIs or any
	// SynchronizationContext-reliant code before AppMain is called: things aren't initialized
	// yet and stuff might break.
	[STAThread]
	public static void Main(string[] args)
	{
		var parser = new DbmlParser();
		var content = File.ReadAllText("E:\\dev\\Apps\\VisualDbml\\VisualDbml\\test.dbml");
		var model = parser.Parse(content);
		
		BuildAvaloniaApp()
			.StartWithClassicDesktopLifetime(args);
	}

	// Avalonia configuration, don't remove; also used by visual designer.
	public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>()
		.UsePlatformDetect()
		.LogToTrace()
		.UseReactiveUI();
}