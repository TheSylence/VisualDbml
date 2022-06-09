using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace VisualDbml.Services;

public class RecentlyUsedFilesService
{
	public IEnumerable<string> ListRecentlyUsedFiles()
	{
		if (!File.Exists(FileName))
			yield break;

		foreach (var line in File.ReadAllLines(FileName))
		{
			yield return line;
		}
	}

	public void AddFile(string filePath)
	{
		var newList = new[] { filePath }
			.Concat(ListRecentlyUsedFiles())
			.Distinct()
			.Take(MaxCount)
			.ToList();

		SaveRecentlyUsedFiles(newList);
	}

	private void SaveRecentlyUsedFiles(IEnumerable<string> list)
	{
		File.WriteAllLines(FileName, list);
	}

	private const int MaxCount = 5;
	private const string FileName = "files.mru";
}