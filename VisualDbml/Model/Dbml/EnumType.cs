using System.Collections.Generic;

namespace VisualDbml.Model.Dbml;

internal record EnumType
{
	public string Name { get; init; }
	public ICollection<string> Members { get; init; }
}