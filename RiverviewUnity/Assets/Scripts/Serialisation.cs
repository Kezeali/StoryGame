using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace NotABear
{

public static class Serialiser
{
	public struct WriteState
	{
		public Token LastToken;
		public int Indent;
	}

	public struct ReadState
	{
		public Token TokenAtCursor;
		public int Cursor;
	}

	public enum Token
	{
		None,
		OpenHeading,
		CloseHeading,
		OpenScope,
		CloseScope,
		Separator,
		Whitespace
	}

	private static string TokenString(Token key)
	{
			return TokenStrings[(int)key];
	}

	private static readonly string[] TokenStrings = new string[]
	{
		"[",
		"]",
		"{",
		"}",
		",",
		" "
	};

	private const string IndentChar = "\t";

	public static WriteState Serialise<T>(WriteState state, T obj)
	{
		StringBuilder sb = new StringBuilder();

		string className = typeof(T).Name;

		if (state.LastToken != Token.None)
		{
			sb.Append(TokenString(Token.Separator));
		}

		WriteIndented(sb, state.Indent, className);

		WriteIndented(sb, state.Indent, TokenString(Token.OpenScope));
		state.Indent += 1;
		sb.AppendLine();

		return state;
	}

	private static WriteState WriteToken(WriteState state, StringBuilder sb, Token token)
	{
		// if last char was newline, indent?
		state.LastToken = token;
		return state;
	}

	private static void WriteIndented(StringBuilder sb, int indent, string value)
	{
		for (int i = 0; i < indent; ++i)
		{
			sb.Append(IndentChar);
		}
		sb.Append(value);
	}

	private static void WriteField(StringBuilder sb)
	{
	}

	public static ReadState Deserialise<T>(ReadState state, ref T loadInto, TypeFactory factory)
	{
		return state;
	}
}

public class TypeFactory
{
	public TypeFactory()
	{
	}
	
	public object BuildType(string name)
	{
		return null;
	}
}

}
