using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace Cloverview
{

public static partial class Serialiser
{
	public struct ReadState
	{
		public Token tokenAtCursor;
		public TokeniserState tokeniserState;
		public char delimeterUsedToOpenCurrentRun;
		public int cursor;
		public Dictionary<System.Type, StructContract> contracts;
	}

	public enum TokeniserState : byte
	{
		Start,
		TypeName,
		Struct,
		Field,
		Array,
		Finish,
		Error
	}

	public class StringReference
	{
		public int beginChar;
		public int endChar;
		public string data;

		public static StringReference Make(ReadState from, ReadState to, string data)
		{
			int fromCursor = from.cursor;
			int toCursor = to.cursor;
			if (fromCursor > toCursor)
			{
				toCursor = fromCursor;
			}

			return new StringReference()
			{
				beginChar = fromCursor,
				endChar = toCursor,
				data = data
			};
		}

		public bool Equals(string str)
		{
			if (str.Length <= this.beginChar - this.endChar)
			{
				return string.Compare(data, beginChar, str, 0, str.Length) == 0;
			}
			else
			{
				return false;
			}
		}

		public override string ToString()
		{
			return data.Substring(beginChar, endChar - beginChar);
		}
	}

	public static ReadState Deserialise<T>(ref T loadInto, string data, DataItemSource dataSource)
	{
		var readState = new ReadState();
		readState.cursor = -1;
		readState.contracts = new Dictionary<System.Type, StructContract>();
		return Deserialise(readState, ref loadInto, data, dataSource);
	}

	public static ReadState Deserialise<T>(ReadState state, ref T loadInto, string data, DataItemSource dataSource)
	{
		//System.Type type = typeof(T);
		//StructContract structContract = AddStructContracts(type, state.contracts);

		//StructContract nextStruct = structContract;
		//int fieldIndex = -1;

		while (state.tokeniserState != TokeniserState.Finish)
		{
			switch (state.tokeniserState)
			{
				case TokeniserState.Start:
				case TokeniserState.TypeName:
				{
					state = ReadToken(state, data);
					switch (state.tokenAtCursor)
					{
						case Token.TypenameTag:
							state.tokeniserState = TokeniserState.TypeName;
						break;
						case Token.Text:
							if (state.tokeniserState == TokeniserState.TypeName)
							{
							}
						break;
					}
				} break;
			}
		}
		return state;
	}

	private static ReadState ReadToken(ReadState state, string data)
	{
		if (state.cursor < -1)
		{
			state.cursor = -1;
		}
		int readIndex = state.cursor+1;
		if (readIndex < data.Length)
		{
			char next = data[readIndex];
			if (char.IsWhiteSpace(next))
			{
				state.tokenAtCursor = Token.Whitespace;
				// consume whitespace
				while (char.IsWhiteSpace(next))
				{
					++readIndex;
					next = data[readIndex];
				}
				--readIndex;
			}
			else
			{
				for (int basicTokenIndex = 0; basicTokenIndex < tokenStrings.Length; ++basicTokenIndex)
				{
					if (next == tokenStrings[basicTokenIndex])
					{
						state.tokenAtCursor = (Token)basicTokenIndex;
					}
				}
			}
			if (state.tokenAtCursor == Token.Escape)
			{
				++readIndex;
				next = data[readIndex];
				state.tokenAtCursor = Token.Text;
			}
			else if (state.tokenAtCursor == Token.None)
			{
				if (char.IsNumber(next))
				{
					state.tokenAtCursor = Token.Number;
				}
				else
				{
					state.tokenAtCursor = Token.Text;
				}
			}
			state.cursor = readIndex;
		}
		return state;
	}

	private static ReadState ParseString(ReadState state, string data)
	{
		return state;
	}
}

}
