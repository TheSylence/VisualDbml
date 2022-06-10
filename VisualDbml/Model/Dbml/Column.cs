namespace VisualDbml.Model.Dbml;

internal record Column(string Name, string Type, bool PrimaryKey, bool Unique, bool NotNull, bool Increment, string? DefaultValue);