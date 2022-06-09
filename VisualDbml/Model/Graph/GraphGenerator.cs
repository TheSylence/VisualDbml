using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using GraphVizNet;
using VisualDbml.Model.Dbml;

namespace VisualDbml.Model.Graph;

internal class GraphGenerator
{
	public string CreateGraph(DbmlModel model)
	{
		var sb = new StringBuilder();
		sb.AppendLine("digraph dbml {");
		sb.AppendLine("layout=\"fdp\"");
		sb.AppendLine("overlap=false");
		sb.AppendLine("splines=ortho");
		sb.AppendLine("bgcolor=\"#44444c\"");
		sb.AppendLine("edge[color=\"#9899A1\"]");
		sb.AppendLine("node[fontcolor=\"#AAAAAA\",color=\"#222222\"]");
		
		foreach (var table in model.Tables)
		{
			AppendTable(table, sb);
		}

		foreach (var enumType in model.Enums)
		{
			AppendEnum(enumType, sb);
		}

		foreach (var relationship in model.Relationships)
		{
			AppendRelationship(relationship, sb);
		}

		foreach (var tableGroup in model.TableGroups)
		{
			AppendTableGroup(tableGroup, sb);
		}
		
		sb.AppendLine("}");
		
		return RenderGraphToFile(sb.ToString());
	}

	private void AppendTableGroup(TableGroup tableGroup, StringBuilder sb)
	{
		sb.Append($"subgraph cluster_{tableGroup.Name} ");
		sb.AppendLine("{");
		sb.AppendLine("color=\"#222222\"");
		sb.AppendLine(string.Join(";", tableGroup.Tables));
		sb.AppendLine("}");
	}

	private void AppendRelationship(Relationship relationship, StringBuilder sb)
	{
		//sb.Append($"{relationship.SourceTable}:{relationship.SourceColumns[0]}");
		sb.Append($"{relationship.SourceTable}");
		sb.Append(" -> ");
		//sb.Append($"{relationship.TargetTable}:{relationship.TargetColumns[0]}");
		sb.Append($"{relationship.TargetTable}");

		sb.AppendLine(" [dir=none];");
	}

	void AppendEnum(EnumType enumType, StringBuilder sb)
	{
		sb.AppendLine($"{enumType.Name} [");
		sb.AppendLine("shape=plain");
		sb.AppendLine("label=<<table border='1' cellborder='0'>");
		sb.AppendLine($"<tr><td><i>{enumType.Name}</i></td></tr>");
		foreach (var member in enumType.Members)
		{
			sb.AppendLine($"<tr><td>{member}</td></tr>");
		}
		sb.AppendLine("</table>>");
		sb.AppendLine("]");
	}

	private void AppendTable(Table table, StringBuilder sb)
	{
		sb.AppendLine($"{table.Name} [");
		sb.AppendLine("shape=plain");
		sb.AppendLine("label=<<table border='1' cellborder='0' bgcolor='#37383F' cellspacing='0'>");
		var displayName = table.Name;
		if (!string.IsNullOrEmpty(table.Alias))
			displayName += $" ({table.Alias})";
		
		sb.AppendLine($"<tr><td colspan='2' bgcolor='#222222'>{displayName}</td></tr>");
		foreach (var column in table.Columns)
		{
			sb.Append($"<tr><td port='{column.Name}' align='left'>");
			if (column.PrimaryKey)
				sb.Append("<b>");
			if (column.Unique)
				sb.AppendLine("<i>");

			sb.Append(column.Name);
			
			if (column.Unique)
				sb.AppendLine("</i>");
			if (column.PrimaryKey)
				sb.Append("</b>");

			sb.Append($"</td><td align='right'>{column.Type}");
			if (!column.NotNull && !column.PrimaryKey)
				sb.Append('?');
			if (column.Increment)
				sb.Append('+');
			sb.AppendLine("</td></tr>");
		}
		sb.AppendLine("</table>>");
		sb.AppendLine("]");
	}

	private static string RenderGraphToFile(string graph)
	{
		var graphViz = new GraphViz();

		var fileName = Path.GetTempFileName();
		graphViz.LayoutAndRenderDotGraph(graph, fileName, "svg");
		return fileName;
	}
}