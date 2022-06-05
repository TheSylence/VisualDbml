using System.Collections.Generic;

namespace VisualDbml.Model;

internal record EnumType
{
	public string Name { get; init; }
	public ICollection<string> Members { get; init; }
}