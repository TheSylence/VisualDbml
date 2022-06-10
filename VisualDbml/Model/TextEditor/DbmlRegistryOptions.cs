using System.Collections.Generic;
using System.IO;
using System.Reflection;
using TextMateSharp.Grammars;
using TextMateSharp.Internal.Grammars.Reader;
using TextMateSharp.Internal.Types;
using TextMateSharp.Registry;
using TextMateSharp.Themes;

namespace VisualDbml.Model.TextEditor;

internal class DbmlRegistryOptions : IRegistryOptions
{
	public DbmlRegistryOptions()
	{
		_wrapped = new RegistryOptions(ThemeName.DarkPlus);
	}

	public IRawTheme GetDefaultTheme()
	{
		var t = _wrapped.GetDefaultTheme();

		return t;
	}

	public IRawGrammar GetGrammar(string scopeName)
	{
		using var stream = Assembly.GetExecutingAssembly()
			.GetManifestResourceStream("VisualDbml.Data.dbml.tmLanguage.json");
		using var reader = new StreamReader(stream!);

		return GrammarReader.ReadGrammarSync(reader);
	}

	public ICollection<string> GetInjections(string scopeName) => _wrapped.GetInjections(scopeName);

	public IRawTheme GetTheme(string scopeName)
	{
		var t = _wrapped.GetTheme(scopeName);

		return t;
	}

	private readonly RegistryOptions _wrapped;
}