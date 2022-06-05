using System.Collections.Generic;

namespace VisualDbml.Model;

internal record Index
{
	public ICollection<string> Columns { get; init; }
	public string Name { get; init; }
	public bool PrimaryKey { get; init; }
	public string Type { get; init; }
	public bool Unique { get; init; }
}