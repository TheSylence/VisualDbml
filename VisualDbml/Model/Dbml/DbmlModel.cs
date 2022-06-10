using System.Collections.Generic;

namespace VisualDbml.Model.Dbml;

internal record DbmlModel(ICollection<Table> Tables, ICollection<EnumType> Enums,
	ICollection<Relationship> Relationships, ICollection<TableGroup> TableGroups);