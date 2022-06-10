using System.Collections.Generic;

namespace VisualDbml.Model.Dbml;

internal record Table(string Alias, string Name, ICollection<Column> Columns, ICollection<Index> Indexes);