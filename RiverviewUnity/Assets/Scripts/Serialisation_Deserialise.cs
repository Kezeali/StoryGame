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
		public Token TokenAtCursor;
		public int Cursor;
		public Dictionary<System.Type, StructContract> contracts;
	}

	public static ReadState Deserialise<T>(ref T loadInto, DataItemSource dataSource)
	{
		var readState = new ReadState();
		readState.contracts = new Dictionary<System.Type, StructContract>();
		return Deserialise(readState, ref loadInto, dataSource);
	}

	public static ReadState Deserialise<T>(ReadState state, ref T loadInto, DataItemSource dataSource)
	{
		return state;
	}
}

}
