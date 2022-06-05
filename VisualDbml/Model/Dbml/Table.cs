using System.Collections.Generic;

namespace VisualDbml.Model;

internal record Table
{
	public string Alias { get; init; }
	public ICollection<Column> Columns { get; init; }
	public ICollection<Index> Indexes { get; init; }
	public string Name { get; init; }
	public string Schema { get; init; }
}