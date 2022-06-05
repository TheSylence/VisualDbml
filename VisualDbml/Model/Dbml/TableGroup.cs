using System.Collections.Generic;

namespace VisualDbml.Model;

internal record TableGroup
{
	public string Name { get; init; }
	public ICollection<string> Tables { get; init; }
}