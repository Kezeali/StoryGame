using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace NotABear
{

public static partial class Serialiser
{
	public struct ReadState
	{
		public Token tokenAtCursor;
		public int cursor;
		public Dictionary<System.Type, StructContract> contracts;
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
		System.Type type = typeof(T);
		StructContract structContract = AddStructContracts(type, state.contracts);

		state = ReadToken(state, data);
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
				state.tokenAtCursor = Token.String;
			}
			state.cursor = readIndex;
		}
		return state;
	}
}

}
