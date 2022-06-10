using System.Collections.Generic;

namespace VisualDbml.Model.Dbml;

internal record TableGroup(ICollection<string> Tables, string Name);