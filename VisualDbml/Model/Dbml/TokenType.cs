namespace VisualDbml.Model;

internal enum TokenType
{
	GroupType,
	GroupingStart,
	GroupingEnd,
	SettingsStart,
	SettingsEnd,
	Setting,
	ColumnType,
	Reference,
	Name,
	Alias,
	RelationshipType,
	IndexColumn,
	EnumMember
}