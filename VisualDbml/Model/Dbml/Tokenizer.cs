using System;
using System.Collections.Generic;
using System.Linq;

namespace VisualDbml.Model.Dbml;

internal static class Tokenizer
{
	public static IEnumerable<Token> Tokenize(string content)
	{
		var parts = content.Split(new[] { " ", Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
		var merged = MergeSettingTokens(parts);
		merged = MergeCompositeColumns(merged);

		return GenerateTokens(merged);
	}

	private static Queue<string> FindNextGroup(Queue<string> tokens)
	{
		var nestingLevel = 0;
		var group = new Queue<string>();

		var groupTypeToken = tokens.Peek();
		if (KeywordList.Contains(groupTypeToken.ToLower()))
		{
			tokens.Dequeue();
			group.Enqueue(groupTypeToken);
			var name = tokens.Peek();
			if (name != "{")
			{
				tokens.Dequeue();
				group.Enqueue(name);
			}
		}

		var next = "";
		while ( tokens.Any() && (next = tokens.Dequeue()) != "{")
		{
			group.Enqueue(next);
		}

		group.Enqueue(next);
		nestingLevel++;

		while (nestingLevel != 0 && tokens.Any())
		{
			next = tokens.Dequeue();
			if (next == "{")
				++nestingLevel;
			else if (next == "}")
				--nestingLevel;

			group.Enqueue(next);
		}

		return group;
	}

	private static IEnumerable<Token> GenerateTokens(IEnumerable<string> merged)
	{
		var queue = new Queue<string>(merged);

		while (queue.Any())
		{
			var groupTokens = FindNextGroup(queue);
			foreach (var token in TokenizeGroup(groupTokens, ""))
			{
				yield return token;
			}
		}
	}

	private static IEnumerable<string> MergeCompositeColumns(IEnumerable<string> tokens)
	{
		var inComposite = false;
		var compositeBuffer = new List<string>();

		foreach (var token in tokens)
		{
			if (inComposite)
			{
				compositeBuffer.Add(token);

				if (token.EndsWith(")") && !token.Contains('('))
				{
					var merged = string.Join(" ", compositeBuffer);
					compositeBuffer.Clear();
					yield return merged;
					inComposite = false;
				}
			}
			else if (token.Contains('(') && !token.Contains(')'))
			{
				inComposite = true;
				compositeBuffer.Add(token);
			}
			else
				yield return token;
		}
	}

	private static IEnumerable<string> MergeSettingTokens(IEnumerable<string> tokens)
	{
		var tokenBuffer = new List<string>();
		var inSettings = false;

		foreach (var token in tokens)
		{
			if (inSettings)
			{
				tokenBuffer.Add(token);

				if (token.EndsWith("]"))
				{
					var merged = string.Join(" ", tokenBuffer);
					tokenBuffer.Clear();
					yield return merged;
					inSettings = false;
				}
			}
			else if (token.StartsWith("["))
			{
				if (!token.EndsWith("]"))
				{
					inSettings = true;
					tokenBuffer.Add(token);
				}
				else
					yield return token;
			}
			else
				yield return token;
		}
	}

	private static IEnumerable<Token> TokenizeTableGroup(Queue<string> tokens)
	{
		yield return new Token(tokens.Dequeue(), TokenType.Name);
		yield return new Token(tokens.Dequeue(), TokenType.GroupingStart);
		
		while ( tokens.Peek() != "}")
		{
			yield return new Token(tokens.Dequeue(), TokenType.Name);
		}

		yield return new Token(tokens.Dequeue(), TokenType.GroupingEnd);
	}

	private static IEnumerable<Token> TokenizeEnum(Queue<string> tokens)
	{
		yield return new Token(tokens.Dequeue(), TokenType.Name);
		yield return new Token(tokens.Dequeue(), TokenType.GroupingStart);

		while ( tokens.Peek() != "}")
		{
			yield return new Token(tokens.Dequeue(), TokenType.EnumMember);
		}

		yield return new Token(tokens.Dequeue(), TokenType.GroupingEnd);
	}
	
	private static IEnumerable<Token> TokenizeGroup(Queue<string> groupTokens, string groupType)
	{
		var groupStarted = false;
		var wasName = false;
		var groupTypeName = groupTokens.Peek();

		if (groupTypeName.ToLower().StartsWith("ref"))
		{
			foreach (var token in TokenizeRef(groupTokens))
			{
				yield return token;
			}

			yield break;
		}

		if (groupTypeName.ToLower() == "table")
		{
			foreach (var token in TokenizeTable(groupTokens, groupTypeName))
			{
				yield return token;
			}
		}

		if (groupType.ToLower() == "indexes")
		{
			foreach (var token in TokenizeIndexes(groupTokens))
			{
				yield return token;
			}
		}
		else if (groupType.ToLower() == "enum")
		{
			foreach (var token in TokenizeEnum(groupTokens))
			{
				yield return token;
			}
		}
		else if (groupType.ToLower() == "tablegroup")
		{
			foreach (var token in TokenizeTableGroup(groupTokens))
			{
				yield return token;
			}
		}

		while (groupTokens.Any())
		{
			var next = groupTokens.Dequeue();
			if (KeywordList.Contains(next.ToLower()))
			{
				yield return new Token(next, TokenType.GroupType);

				var nextGroup = FindNextGroup(groupTokens);
				foreach (var token in TokenizeGroup(nextGroup, next))
				{
					yield return token;
				}
			}
			else if (next == "{")
			{
				groupStarted = true;
				yield return new Token(next, TokenType.GroupingStart);
			}
			else if (next == "}")
				yield return new Token(next, TokenType.GroupingEnd);
			else if (next.StartsWith("["))
			{
				yield return new Token("[", TokenType.SettingsStart);
				var parts = next.Trim('[', ']').Split(',').Select(p => p.Trim());
				foreach (var part in parts)
				{
					if (part.StartsWith("ref:"))
						yield return new Token(part, TokenType.Reference);
					else
						yield return new Token(part, TokenType.Setting);
				}

				yield return new Token("]", TokenType.SettingsEnd);
			}
			else if (wasName && groupStarted)
			{
				yield return new Token(next, TokenType.ColumnType);
				wasName = false;
			}
			else
			{
				wasName = true;
				yield return new Token(next, TokenType.Name);
			}
		}
	}

	private static IEnumerable<Token> TokenizeIndexes(Queue<string> tokens)
	{
		yield return new Token(tokens.Dequeue(), TokenType.GroupingStart);

		while (tokens.Any())
		{
			yield return new Token(tokens.Dequeue(), TokenType.IndexColumn);

			if (tokens.Peek().Contains('['))
			{
				var settings = tokens.Dequeue().Trim('[', ']', ' ');
				yield return new Token("[", TokenType.SettingsStart);
				var parts = settings.Split(',').Select(p => p.Trim());
				foreach (var part in parts)
				{
					yield return new Token(part, TokenType.Setting);
				}

				yield return new Token("]", TokenType.SettingsEnd);
			}
			else if (tokens.Peek() == "}")
				yield return new Token(tokens.Dequeue(), TokenType.GroupingEnd);
		}
	}

	private static IEnumerable<Token> TokenizeRef(Queue<string> tokens)
	{
		var next = tokens.Dequeue();
		if (next.EndsWith(":"))
		{
			foreach (var token in TokenizeShortRef(tokens))
			{
				yield return token;
			}

			yield break;
		}

		yield return new Token(next, TokenType.GroupType);

		next = tokens.Dequeue();
		if (next != "{")
		{
			yield return new Token(next, TokenType.Name);
			next = tokens.Dequeue();
		}

		yield return new Token(next, TokenType.GroupingStart);
		yield return new Token(tokens.Dequeue(), TokenType.Name);
		yield return new Token(tokens.Dequeue(), TokenType.RelationshipType);
		yield return new Token(tokens.Dequeue(), TokenType.Name);

		if (tokens.Peek().StartsWith("["))
		{
			yield return new Token("[", TokenType.SettingsStart);

			var settings = tokens.Dequeue().Trim('[', ']');

			foreach (var setting in settings.Split(',').Select(s => s.Trim()))
			{
				yield return new Token(setting, TokenType.Setting);
			}

			yield return new Token("]", TokenType.SettingsEnd);
		}

		yield return new Token(tokens.Dequeue(), TokenType.GroupingEnd);
	}

	private static IEnumerable<Token> TokenizeShortRef(Queue<string> tokens)
	{
		yield return new Token("ref", TokenType.GroupType);

		yield return new Token("{", TokenType.GroupingStart);
		yield return new Token(tokens.Dequeue(), TokenType.Name);
		yield return new Token(tokens.Dequeue(), TokenType.RelationshipType);
		yield return new Token(tokens.Dequeue(), TokenType.Name);

		if (tokens.Any() && tokens.Peek().StartsWith("["))
		{
			yield return new Token("[", TokenType.SettingsStart);
			var settings = tokens.Dequeue().Trim('[', ']');

			foreach (var setting in settings.Split(',').Select(s => s.Trim()))
			{
				yield return new Token(setting, TokenType.Setting);
			}

			yield return new Token("]", TokenType.SettingsEnd);
		}

		yield return new Token("}", TokenType.GroupingEnd);

		if (tokens.Any())
		{
			foreach (var token in TokenizeGroup(tokens, ""))
			{
				yield return token;
			}
		}
	}

	private static IEnumerable<Token> TokenizeTable(Queue<string> groupTokens, string groupTypeName)
	{
		groupTokens.Dequeue();
		yield return new Token(groupTypeName, TokenType.GroupType);

		var nameTokens = new List<string>();
		var hasAlias = false;

		string next;
		while ((next = groupTokens.Peek()) != "{")
		{
			if (next == "as")
			{
				hasAlias = true;
				groupTokens.Dequeue();
				continue;
			}

			nameTokens.Add(groupTokens.Dequeue());
		}

		yield return new Token(nameTokens.First(), TokenType.Name);
		if (hasAlias)
			yield return new Token(nameTokens.Last(), TokenType.Alias);
	}

	private static readonly string[] KeywordList =
	{
		"project", "table", "enum", "tablegroup", "ref", "indexes"
	};
}