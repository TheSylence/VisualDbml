using System.Collections.Generic;

namespace VisualDbml.Model.Dbml;

internal record EnumType(ICollection<string> Members, string Name);