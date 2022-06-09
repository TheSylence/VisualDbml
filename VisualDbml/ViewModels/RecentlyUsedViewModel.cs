namespace VisualDbml.ViewModels;

public class RecentlyUsedViewModel : ViewModelBase
{
	public RecentlyUsedViewModel(string filePath)
	{
		FilePath = filePath;
		FileName = System.IO.Path.GetFileName(filePath);
	}

	public string FileName { get; }
	public string FilePath { get; }
}