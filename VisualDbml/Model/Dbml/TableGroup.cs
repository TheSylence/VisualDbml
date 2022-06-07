using System.Collections.Generic;

namespace VisualDbml.Model.Dbml;

internal record TableGroup
{
	public string Name { get; init; }
	public ICollection<string> Tables { get; init; }
}