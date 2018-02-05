using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace NotABear
{

public static partial class Serialiser
{
	public struct WriteState
	{
		public Token lastToken;
		public int indent;
		public bool useFormatting;
		public Dictionary<System.Type, StructContract> contracts; 
		public DataItemSource dataSource;
	}

	private const string IndentChar = "\t";

	public static WriteState Serialise<T>(StringBuilder sb, T obj, DataItemSource dataSource)
	{
		WriteState state = new WriteState();
		state.useFormatting = true;
		state.contracts = new Dictionary<System.Type, StructContract>();
		state.dataSource = dataSource;

		System.Type type = typeof(T);
		return Serialise(state, sb, obj, type);
	}

	public static WriteState Serialise<T>(StringBuilder sb, WriteState state, T obj)
	{
		System.Type type = typeof(T);
		return Serialise(state, sb, obj, type);
	}

	public static WriteState Serialise(WriteState state, StringBuilder sb, object obj, System.Type type)
	{
		StructContract structContract = AddStructContracts(type, state.contracts);

		if (state.lastToken != Token.None && state.lastToken != Token.Separator)
		{
			state = WriteFormatted(state, sb, Token.Separator);
		}

		state = WriteStruct(state, sb, obj, structContract);

		return state;
	}

	private static WriteState WriteStruct(WriteState state, StringBuilder sb, object obj, StructContract contract)
	{
		System.Type type = contract.type;

		state = WriteFormatted(state, sb, Token.TypenameTag);

		string className = type.Name;
		state = WriteFormatted(state, sb, className);
		state.lastToken = Token.Text;

		state = WriteFormatted(state, sb, Token.OpenScope);

		if (obj != null)
		{
			for (int fieldIndex = 0; fieldIndex < contract.fields.Length; ++fieldIndex)
			{
				FieldContract fieldContract = contract.fields[fieldIndex];
				state = WriteField(state, sb, obj, fieldContract);
			}
		}

		state = WriteFormatted(state, sb, Token.CloseScope);

		return state;
	}

	private static WriteState WriteField(WriteState state, StringBuilder sb, object obj, FieldContract contract)
	{
		if (state.lastToken != Token.OpenScope)
		{
			state = WriteFormatted(state, sb, Token.Separator);

			if (state.useFormatting)
			{
				sb.AppendLine();
				state.lastToken = Token.Whitespace;
			}
		}

		FieldInfo fieldInfo = contract.fieldInfo;
		state = WriteFormatted(state, sb, fieldInfo.Name);

		state = WriteFormatted(state, sb, Token.KeyValueSeparator);

		state.indent += 1;

		object value = fieldInfo.GetValue(obj);
		state = WriteFieldValue(state, sb, value, contract);

		state.indent -= 1;

		return state;
	}

	private static WriteState WriteFieldValue(WriteState state, StringBuilder sb, object value, FieldContract contract)
	{
		ValueType type = contract.type;
		switch (type)
		{
			case ValueType.String:
			{
				state = WriteValue(state, sb, (string)value);
			} break;
			case ValueType.Float32:
			{
				state = WriteValue(state, sb, (System.Single)value);
			} break;
			case ValueType.Float64:
			{
				state = WriteValue(state, sb, (System.Double)value);
			} break;
			case ValueType.Bool:
			{
				state = WriteValue(state, sb, (System.Boolean)value);
			} break;
			case ValueType.Int8:
			{
				state = WriteValue(state, sb, (System.SByte)value);
			} break;
			case ValueType.Int16:
			{
				state = WriteValue(state, sb, (System.Int16)value);
			} break;
			case ValueType.Int32:
			{
				state = WriteValue(state, sb, (System.Int32)value);
			} break;
			case ValueType.Int64:
			{
				state = WriteValue(state, sb, (System.Int64)value);
			} break;
			case ValueType.UInt8:
			{
				state = WriteValue(state, sb, (System.Byte)value);
			} break;
			case ValueType.UInt16:
			{
				state = WriteValue(state, sb, (System.UInt16)value);
			} break;
			case ValueType.UInt32:
			{
				state = WriteValue(state, sb, (System.UInt32)value);
			} break;
			case ValueType.UInt64:
			{
				state = WriteValue(state, sb, (System.UInt64)value);
			} break;
			case ValueType.DateTime:
			{
				state = WriteValue(state, sb, (System.DateTime)value);
			} break;
			case ValueType.Array:
			{
				var enumerableValue = value as IEnumerable;
				FieldContract elementContract = contract.elementContract;

				bool alreadyIndented = state.indent > 0;
				if (alreadyIndented)
				{
					state.indent -= 1;
				}

				state = WriteFormatted(state, sb, Token.OpenArray);

				state.indent += 1;

				if (enumerableValue != null)
				{
					foreach (var element in enumerableValue)
					{
						if (state.lastToken != Token.OpenArray)
						{
							state = WriteFormatted(state, sb, Token.Separator);

							if (state.useFormatting)
							{
								sb.Append(" ");
								state.lastToken = Token.Whitespace;
							}
						}

						state = WriteFieldValue(state, sb, element, elementContract);
					}
				}

				state.indent -= 1;

				state = WriteFormatted(state, sb, Token.CloseArray);

				if (alreadyIndented)
				{
					state.indent += 1;
				}
			} break;
			case ValueType.Map:
			{
				Debug.LogError("Not supported yet: " + type);
			} break;
			case ValueType.Struct:
			{
				state = WriteStruct(state, sb, value, contract.structContract);
			} break;
			case ValueType.Enum:
			{
				state = WriteValue(state, sb, value.ToString());
			} break;
			case ValueType.DataItem:
			{
				var dataItemValue = value as DataItem;
				state = WriteFormatted(state, sb, Token.DataItemTag);
				state = WriteFormatted(state, sb, dataItemValue.name);
				state.lastToken = Token.Text;
			} break;
		}
		return state;
	}

	private static WriteState WriteValue(WriteState state, StringBuilder sb, string value)
	{
		state = WriteFormatted(state, sb, value);
		return state;
	}

	private static WriteState WriteValue(WriteState state, StringBuilder sb, System.Boolean value)
	{
		state = WriteFormatted(state, sb, value ? "true" : "false");
		return state;
	}

	private static WriteState WriteValue(WriteState state, StringBuilder sb, System.Single value)
	{
		state = WriteFormatting(state, sb);
		sb.AppendFormat("{0:R}", value);
		return state;
	}

	private static WriteState WriteValue(WriteState state, StringBuilder sb, System.Double value)
	{
		state = WriteFormatting(state, sb);
		sb.AppendFormat("{0:R}", value);
		return state;
	}

	private static WriteState WriteValue(WriteState state, StringBuilder sb, System.SByte value)
	{
		state = WriteFormatting(state, sb);
		sb.Append(value.ToString());
		return state;
	}

	private static WriteState WriteValue(WriteState state, StringBuilder sb, System.Int16 value)
	{
		state = WriteFormatting(state, sb);
		sb.Append(value.ToString());
		return state;
	}

	private static WriteState WriteValue(WriteState state, StringBuilder sb, System.Int32 value)
	{
		state = WriteFormatting(state, sb);
		sb.Append(value.ToString());
		return state;
	}

	private static WriteState WriteValue(WriteState state, StringBuilder sb, System.Int64 value)
	{
		state = WriteFormatting(state, sb);
		sb.Append(value.ToString());
		return state;
	}

	private static WriteState WriteValue(WriteState state, StringBuilder sb, System.Byte value)
	{
		state = WriteFormatting(state, sb);
		sb.Append(value.ToString());
		return state;
	}

	private static WriteState WriteValue(WriteState state, StringBuilder sb, System.UInt16 value)
	{
		state = WriteFormatting(state, sb);
		sb.Append(value.ToString());
		return state;
	}

	private static WriteState WriteValue(WriteState state, StringBuilder sb, System.UInt32 value)
	{
		state = WriteFormatting(state, sb);
		sb.Append(value.ToString());
		return state;
	}

	private static WriteState WriteValue(WriteState state, StringBuilder sb, System.UInt64 value)
	{
		state = WriteFormatting(state, sb);
		sb.Append(value.ToString());
		return state;
	}

	private static WriteState WriteValue(WriteState state, StringBuilder sb, System.DateTime value)
	{
		state = WriteFormatting(state, sb);
		sb.AppendFormat("{0:O}", value);
		return state;
	}

	private static WriteState WriteFormatted(WriteState state, StringBuilder sb, Token token)
	{
		if (state.useFormatting)
		{
			switch (token)
			{
				case Token.TypenameTag:
				case Token.OpenScope:
				{
					if (state.lastToken != Token.None)
					{
						sb.AppendLine();
					}
				} break;
				case Token.CloseScope:
				{
					if (state.lastToken != Token.None && state.lastToken != Token.Whitespace)
					{
						sb.AppendLine();
					}
					state.indent -= 1;
				} break;
				case Token.CloseArray:
				{
					if (state.lastToken != Token.None && state.lastToken != Token.Whitespace)
					{
						sb.AppendLine();
					}
				} break;
			}
		}
		state = WriteFormatted(state, sb, TokenString(token));
		state.lastToken = token;
		if (state.useFormatting)
		{
			switch (token)
			{
				case Token.OpenScope:
				{
					state.indent += 1;
				} break;
			}
		}
		return state;
	}

	private static WriteState WriteFormatted(WriteState state, StringBuilder sb, string value)
	{
		state = WriteFormatting(state, sb);
		sb.Append(value);
		state = WritePostFormatting(state, sb);
		return state;
	}

	private static WriteState WriteFormatting(WriteState state, StringBuilder sb)
	{
		if (state.useFormatting)
		{
			bool newline;
			switch (state.lastToken)
			{
				case Token.OpenScope:
				{
					sb.AppendLine();
					state.lastToken = Token.Whitespace;
				} break;
			}
			if (sb.Length > 0)
			{
				char prevChar = sb[sb.Length-1];
				switch (prevChar)
				{
					case '\n':
					case '\r':
					{
						newline = true;
					} break;
					default:
					{
						newline = false;
					} break;
				}
			}
			else
			{
				newline = true;
			}
			if (newline)
			{
				WriteIndent(sb, state.indent);
			}
		}
		return state;
	}

	private static WriteState WritePostFormatting(WriteState state, StringBuilder sb)
	{
		return state;
	}

	private static void WriteIndented(StringBuilder sb, int indent, string value)
	{
		WriteIndent(sb, indent);
		sb.Append(value);
	}

	private static void WriteIndent(StringBuilder sb, int indent)
	{
		for (int i = 0; i < indent; ++i)
		{
			sb.Append(IndentChar);
		}
	}
}

}
