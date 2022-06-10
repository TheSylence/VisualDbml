using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;

namespace VisualDbml.Model.Dbml;

internal class DbmlParser
{
	public DbmlModel? Parse(string content)
	{
		content = RemoveComments(content);
		content = Trim(content);

		var tokens = new Queue<Token>(Tokenizer.Tokenize(content));

		return GenerateModel(tokens);
	}

	private DbmlModel? GenerateModel(Queue<Token> tokens)
	{
		var tables = new List<Table>();
		var relationships = new List<Relationship>();
		var enums = new List<EnumType>();
		var tableGroups = new List<TableGroup>();

		while (tokens.Any())
		{
			var next = tokens.Dequeue();
			if (next.Type != TokenType.GroupType)
				return null;

			switch (next.Content.ToLower())
			{
				case "table":
					var table = ParseTable(tokens, out var refs);
					if (table == null)
						return null;
					
					tables.Add(table);
					relationships.AddRange(refs);
					break;
				case "enum":
					enums.Add(ParseEnum(tokens));
					break;
				case "tablegroup":
					tableGroups.Add(ParseTableGroup(tokens));
					break;
				case "ref":
					relationships.Add(ParseRelationship(tokens));
					break;
				default:
					while (tokens.Dequeue().Type != TokenType.GroupingEnd)
					{
					}

					break;
			}
		}

		return new DbmlModel(Tables: tables, Enums: enums, Relationships: relationships, TableGroups: tableGroups);
	}

	private static Column? ParseColumn(Queue<Token> tokens, string tableName, out Relationship? columnRef)
	{
		columnRef = null;
		
		var next = tokens.Dequeue();
		if (next.Type != TokenType.Name)
			return null;
		
		var name = next.Content;

		next = tokens.Dequeue();
		if (next.Type != TokenType.ColumnType)
			return null;
		
		var type = next.Content;

		var primary = false;
		var unique = false;
		var increment = false;
		var notNull = false;
		string? defaultValue = null;

		next = tokens.Peek();
		if (next.Type == TokenType.SettingsStart)
		{
			(primary, notNull, increment, unique, defaultValue, columnRef) =
				ParseColumnSettings(tokens, name, tableName);
		}

		return new Column(Name: name, Type: type, PrimaryKey: primary, Unique: unique, NotNull: notNull,
			Increment: increment, DefaultValue: defaultValue);
	}

	private static (bool primary, bool notNull, bool increment, bool unique, string? defaultValue, Relationship?)
		ParseColumnSettings(Queue<Token> tokens, string columnName, string tableName)
	{
		var next = tokens.Dequeue();
		Debug.Assert(next.Type == TokenType.SettingsStart);

		var primary = false;
		var unique = false;
		var increment = false;
		var notNull = false;
		string? defaultValue = null;
		Relationship? columnRef = null;

		while (tokens.Peek().Type == TokenType.Setting)
		{
			next = tokens.Dequeue();
			var setting = next.Content.ToLower();

			if (setting == "pk")
				primary = true;
			else if (setting == "unique")
				unique = true;
			else if (setting == "increment")
				increment = true;
			else if (setting == "not null")
				notNull = true;
		}

		if (tokens.Peek().Type == TokenType.Reference)
		{
			next = tokens.Dequeue();
			var parts = next.Content.Replace("ref:", "").Trim().Split(' ');

			var type = ParseRelationshipType(parts[0]);
			var (table, columns) = ParseComposite(parts[1]);

			columnRef = new Relationship(type: type, targetTable: table, targetColumns: columns,
				sourceColumns: new[] { columnName }, sourceTable: tableName);
		}

		tokens.Dequeue();

		return (primary, notNull, increment, unique, defaultValue, columnRef);
	}

	private static (string, List<string>) ParseComposite(string content)
	{
		var parts = content.Split('.');
		var columns = parts[1].Trim('(', ')').Split(',').Select(t => t.Trim()).ToList();

		return (parts[0], columns);
	}

	private static TableGroup ParseTableGroup(Queue<Token> tokens)
	{
		var next = tokens.Dequeue();
		Debug.Assert(next.Type == TokenType.Name);
		var name = next.Content;

		next = tokens.Dequeue();
		Debug.Assert(next.Type == TokenType.GroupingStart);
		
		var tables = new List<string>();
		
		while ( tokens.Peek().Type != TokenType.GroupingEnd )
		{
			next = tokens.Dequeue();
			Debug.Assert(next.Type ==  TokenType.Name);
			tables.Add(next.Content);
		}

		tokens.Dequeue();
		return new TableGroup(Tables: tables, Name: name);
	}
	private static EnumType ParseEnum(Queue<Token> tokens)
	{
		var next = tokens.Dequeue();
		Debug.Assert(next.Type == TokenType.Name);
		var name = next.Content;

		next = tokens.Dequeue();
		Debug.Assert(next.Type == TokenType.GroupingStart);

		var members = new List<string>();
		
		while ( tokens.Peek().Type != TokenType.GroupingEnd )
		{
			next = tokens.Dequeue();
			Debug.Assert(next.Type ==  TokenType.EnumMember);
			members.Add(next.Content);
		}

		tokens.Dequeue();
		return new EnumType(Members: members, Name: name);
	}

	private static IEnumerable<Index> ParseIndexes(Queue<Token> tokens)
	{
		var next = tokens.Dequeue();
		Debug.Assert(next.Type == TokenType.GroupType);

		next = tokens.Dequeue();
		Debug.Assert(next.Type == TokenType.GroupingStart);

		while ((next = tokens.Dequeue()).Type != TokenType.GroupingEnd)
		{
			Debug.Assert(next.Type == TokenType.IndexColumn);
			var columns = next.Content.Trim('(', ')').Split(',').Select(p => p.Trim());

			var primaryKey = false;
			var unique = false;
			var name = "";
			var type = "";

			if (tokens.Peek().Type == TokenType.SettingsStart)
			{
				tokens.Dequeue();

				while ((next = tokens.Dequeue()).Type != TokenType.SettingsEnd)
				{
					Debug.Assert(next.Type == TokenType.Setting);
					var setting = next.Content.Split(':').Select(p => p.Trim()).ToList();

					switch (setting[0].ToLower())
					{
						case "type":
							type = setting[1];
							break;
						case "name":
							name = setting[1];
							break;
						case "unique":
							unique = true;
							break;
						case "pk":
							primaryKey = true;
							break;
					}
				}
			}

			yield return new Index(Columns: columns.ToList(), PrimaryKey: primaryKey, Name: name, Type: type,
				Unique: unique);
		}
	}

	private static Relationship ParseRelationship(Queue<Token> tokens)
	{
		var next = tokens.Dequeue();
		if (next.Type == TokenType.Name)
			tokens.Dequeue();
		else if (next.Type != TokenType.GroupingStart)
			Debug.Fail("Expected grouping start");

		next = tokens.Dequeue();
		Debug.Assert(next.Type == TokenType.Name);
		var (sourceTable, sourceColumns) = ParseComposite(next.Content);

		next = tokens.Dequeue();
		Debug.Assert(next.Type == TokenType.RelationshipType);
		var type = ParseRelationshipType(next.Content);

		next = tokens.Dequeue();
		Debug.Assert(next.Type == TokenType.Name);
		var (targetTable, targetColumns) = ParseComposite(next.Content);

		var updateSettings = RelationshipSetting.NoAction;
		var deleteSettings = RelationshipSetting.NoAction;

		next = tokens.Dequeue();
		if (next.Type == TokenType.SettingsStart)
		{
			while (tokens.Peek().Type == TokenType.Setting)
			{
				next = tokens.Dequeue();

				var parts = next.Content.Split(':').Select(p => p.Trim()).ToList();

				switch (parts[0].ToLower())
				{
					case "update":
						updateSettings = ParseRelationshipSettings(parts[1]);
						break;
					case "delete":
						deleteSettings = ParseRelationshipSettings(parts[1]);
						break;
				}
			}

			next = tokens.Dequeue();
			Debug.Assert(next.Type == TokenType.SettingsEnd);

			next = tokens.Dequeue();
		}

		Debug.Assert(next.Type == TokenType.GroupingEnd);

		return new Relationship(Type: type, SourceTable: sourceTable, SourceColumns: sourceColumns,
			TargetTable: targetTable, TargetColumns: targetColumns, UpdateSettings: updateSettings,
			DeleteSettings: deleteSettings);
	}

	private static RelationshipSetting ParseRelationshipSettings(string setting)
	{
		return setting.ToLower() switch
		{
			"cascade" => RelationshipSetting.Cascade,
			"restrict" => RelationshipSetting.Restrict,
			"set null" => RelationshipSetting.SetNull,
			"set default" => RelationshipSetting.SetDefault,
			_ => RelationshipSetting.NoAction
		};
	}

	private static RelationshipType ParseRelationshipType(string type)
	{
		return type switch
		{
			"-" => RelationshipType.OneToOne,
			"<" => RelationshipType.OneToMany,
			">" => RelationshipType.ManyToOne,
			"<>" => RelationshipType.ManyToMany,
			_ => throw new ArgumentException()
		};
	}

	private static Table? ParseTable(Queue<Token> tokens, out List<Relationship> relationships)
	{
		var next = tokens.Dequeue();
		Debug.Assert(next.Type == TokenType.Name);
		var name = next.Content;

		var alias = "";
		next = tokens.Dequeue();
		if (next.Type == TokenType.Alias)
		{
			alias = next.Content;
			next = tokens.Dequeue();
		}

		Debug.Assert(next.Type == TokenType.GroupingStart);

		var columns = new List<Column>();
		var indexes = new List<Index>();
		relationships = new List<Relationship>();

		while (tokens.Any())
		{
			var countBefore = tokens.Count;
			
			next = tokens.Peek();

			if (next.Type == TokenType.Name)
			{
				var column = ParseColumn(tokens, name, out var columnRef);
				if (column == null)
					return null;
				
				columns.Add(column);
				if (columnRef != null)
					relationships.Add(columnRef);
			}
			else if (next.Type == TokenType.GroupType)
				indexes.AddRange(ParseIndexes(tokens));
			else if (next.Type == TokenType.GroupingEnd)
			{
				tokens.Dequeue();
				break;
			}

			if(countBefore == tokens.Count)
				return null;
		}

		return new Table(Alias: alias, Name: name, Columns: columns, Indexes: indexes);
	}


	private static string RemoveComments(string content)
	{
		foreach (Match match in SingleLineCommentPattern.Matches(content))
		{
			foreach (Capture capture in match.Captures)
			{
				content = content.Replace(capture.Value, Environment.NewLine);
			}
		}

		foreach (Match match in MultiLineCommentPattern.Matches(content))
		{
			foreach (Capture capture in match.Captures)
			{
				content = content.Replace(capture.Value, "");
			}
		}

		return content;
	}

	private static string Trim(string content)
	{
		var lines = content.Split(Environment.NewLine);

		return string.Join(Environment.NewLine, lines.Select(l => l.Trim()).Where(l => !string.IsNullOrWhiteSpace(l)));
	}

	private static readonly Regex MultiLineCommentPattern =
		new("\\/\\*.*?\\*\\/", RegexOptions.Singleline | RegexOptions.Compiled);

	private static readonly Regex SingleLineCommentPattern = new("\\/\\/.*\\r?\\n", RegexOptions.Compiled);
}