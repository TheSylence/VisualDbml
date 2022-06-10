using System.Diagnostics;
using System.Windows.Input;
using ReactiveUI;

namespace VisualDbml.ViewModels;

public static class Commands
{
	public static ICommand OpenUrlCommand { get; } = ReactiveCommand.Create<string>(OpenUrl);

	private static void OpenUrl(string url)
	{
		if (!url.StartsWith("http"))
			url = "https://" + url;

		Process.Start(new ProcessStartInfo(url)
		{
			UseShellExecute = true
		});
	}
}