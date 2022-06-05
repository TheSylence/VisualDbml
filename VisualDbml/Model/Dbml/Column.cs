namespace VisualDbml.Model;

internal record Column
{
	public string? DefaultValue { get; init; }
	public bool Increment { get; init; }
	public string Name { get; init; }
	public bool NotNull { get; init; }
	public bool PrimaryKey { get; init; }
	public string Type { get; init; }
	public bool Unique { get; init; }
}