using System.Collections.Generic;

namespace VisualDbml.Model.Dbml;

internal record Index(ICollection<string> Columns, bool PrimaryKey, string Name, string Type, bool Unique);