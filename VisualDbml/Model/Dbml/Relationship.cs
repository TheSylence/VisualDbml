using System.Collections.Generic;

namespace VisualDbml.Model;

internal record Relationship
{
	public RelationshipSetting DeleteSettings { get; init; }
	public IList<string> SourceColumns { get; init; }
	public string SourceTable { get; init; }
	public IList<string> TargetColumns { get; init; }
	public string TargetTable { get; init; }
	public RelationshipType Type { get; init; }
	public RelationshipSetting UpdateSettings { get; init; }
}