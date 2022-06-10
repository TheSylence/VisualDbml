using System.Collections.Generic;

namespace VisualDbml.Model.Dbml;

internal record Relationship(RelationshipType Type, string SourceTable, IList<string> SourceColumns, string TargetTable,
	IList<string> TargetColumns, RelationshipSetting UpdateSettings, RelationshipSetting DeleteSettings)
{
	public Relationship(RelationshipType type, string targetTable, IList<string> targetColumns,
		IList<string> sourceColumns, string sourceTable)
		: this(type, sourceTable, sourceColumns, targetTable, targetColumns, RelationshipSetting.NoAction,
			RelationshipSetting.NoAction)
	{
	}
}