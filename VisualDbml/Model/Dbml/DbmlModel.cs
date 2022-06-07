using System.Collections.Generic;

namespace VisualDbml.Model.Dbml;

internal record DbmlModel
{
	public ICollection<EnumType> Enums { get; init; }
	public ICollection<Relationship> Relationships { get; init; }
	public ICollection<TableGroup> TableGroups { get; init; }
	public ICollection<Table> Tables { get; init; }
}