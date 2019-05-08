using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;

// Things to serialize save games.
// This is no longer used (replace with YamlDotNet).

namespace Cloverview
{

public class DataItemSource
{
	private Dictionary<string, DataItem> dataItems = new Dictionary<string, DataItem>();

	public void AddDataItemRange<T>(T[] items)
		where T : DataItem
	{
		for (int i = 0; i < items.Length; ++i)
		{
			AddDataItem(items[i].name, items[i]);
		}
	}

	public void AddDataItem(string name, DataItem item)
	{
		dataItems[name] = item;
	}
	
	public DataItem GetItem(string name)
	{
		return dataItems[name];
	}
}

public static partial class Serialiser
{
	public enum Token : byte
	{
		None,
		OpenArray,
		CloseArray,
		OpenScope,
		CloseScope,
		Separator,
		KeyValueSeparator,
		TypenameTag,
		DataItemTag,
		Escape,
		StringDelim,
		StringDelimAlt,
		Whitespace,
		Text,
		Number,
	}

	private static readonly char[] tokenStrings = new char[]
	{
		'\0',
		'[',
		']',
		'{',
		'}',
		',',
		':',
		'#',
		'^',
		'\\',
		'"',
		'\''
	};

	public enum ValueType
	{
		String,
		Float32,
		Float64,
		Bool,
		Int8,
		Int16,
		Int32,
		Int64,
		UInt8,
		UInt16,
		UInt32,
		UInt64,
		DateTime,
		Array,
		Map,
		Struct,
		Enum,
		DataItem,
	}

	private static string TokenString(Token key)
	{
		return tokenStrings[(int)key].ToString();
	}

	public class StructContract
	{
		public System.Type type;
		public FieldContract[] fields = new FieldContract[0];
	}

	public class FieldContract
	{
		public ValueType type;
		public FieldInfo fieldInfo;
		public StructContract structContract;
		// Note: used for array & map fields
		public FieldContract keyContract;
		public FieldContract elementContract;
	}

	private static StructContract AddStructContracts(System.Type type, Dictionary<System.Type, StructContract> contracts)
	{
		StructContract result = null;
		if (!contracts.TryGetValue(type, out result))
		{
			result = new StructContract();
			contracts.Add(type, result);

			result.type = type;

			// Recursively build contracts for all the fields & any more types they reference
			List<FieldContract> fields = new List<FieldContract>();

			FieldInfo[] fieldInfos = type.GetFields();
			for (int fieldIndex = 0; fieldIndex < fieldInfos.Length; ++fieldIndex)
			{
				FieldInfo fieldInfo = fieldInfos[fieldIndex];
				bool serialized = false;

				serialized = !fieldInfo.IsStatic && !fieldInfo.IsLiteral && !fieldInfo.IsNotSerialized;

				object[] customAttributes = fieldInfos[fieldIndex].GetCustomAttributes(true);
				for (int attributeIndex = 0; attributeIndex < customAttributes.Length; ++attributeIndex)
				{
					var attr = customAttributes[attributeIndex];
					if (attr.GetType() == typeof(System.NonSerializedAttribute))
					{
						serialized = false;
					}
					else if (attr.GetType() == typeof(SerializeField))
					{
						serialized = true;
					}
				}
				if (serialized)
				{
					System.Type fieldType = fieldInfo.FieldType;

					FieldContract fieldContract = BuildFieldContract(fieldType, contracts);
					if (fieldContract != null)
					{
						fieldContract.fieldInfo = fieldInfo;
						fields.Add(fieldContract);
					}
				}
			}
			result.fields = fields.ToArray();
		}
		else
		{
			if (result.fields == null)
			{
				Debug.LogError("Recursive types not supported yet");
			}
		}
		return result;
	}

	private static FieldContract BuildFieldContract(System.Type fieldType, Dictionary<System.Type, StructContract> contracts)
	{
		FieldContract fieldContract = null;
		if (fieldType.IsSubclassOf(typeof(DataItem)))
		{
			fieldContract = new FieldContract();
			fieldContract.type = ValueType.DataItem;
		}
		else if (fieldType.IsArray)
		{
			fieldContract = new FieldContract();
			fieldContract.type = ValueType.Array;
			System.Type elementType = fieldType.GetElementType();
			fieldContract.elementContract = BuildFieldContract(elementType, contracts);
		}
		else if (fieldType.IsEnum)
		{
			fieldContract = new FieldContract();
			fieldContract.type = ValueType.Enum;
		}
		else if (fieldType == typeof(string))
		{
			fieldContract = new FieldContract();
			fieldContract.type = ValueType.String;
		}
		else if (fieldType.IsPrimitive)
		{
			fieldContract = new FieldContract();
			if (fieldType == typeof(bool))
			{
				fieldContract.type = ValueType.Bool;
			}
			else if (fieldType == typeof(System.Single))
			{
				fieldContract.type = ValueType.Float32;
			}
			else if (fieldType == typeof(System.Double))
			{
				fieldContract.type = ValueType.Float64;
			}
			else if (fieldType == typeof(sbyte))
			{
				fieldContract.type = ValueType.Int8;
			}
			else if (fieldType == typeof(System.Int16))
			{
				fieldContract.type = ValueType.Int16;
			}
			else if (fieldType == typeof(System.Int32))
			{
				fieldContract.type = ValueType.Int32;
			}
			else if (fieldType == typeof(System.Int64))
			{
				fieldContract.type = ValueType.Int64;
			}
			else if (fieldType == typeof(byte))
			{
				fieldContract.type = ValueType.UInt8;
			}
			else if (fieldType == typeof(System.UInt16))
			{
				fieldContract.type = ValueType.UInt16;
			}
			else if (fieldType == typeof(System.UInt32))
			{
				fieldContract.type = ValueType.UInt32;
			}
			else if (fieldType == typeof(System.UInt64))
			{
				fieldContract.type = ValueType.UInt64;
			}
			else
			{
				fieldContract = null;
				Debug.Log("Unsupported primative type: " + fieldType.ToString());
			}
		}
		else if (fieldType.IsClass && !fieldType.IsEnum)
		{
			if (fieldType.IsSerializable)
			{
				fieldContract = new FieldContract();
				fieldContract.type = ValueType.Struct;
				fieldContract.structContract = AddStructContracts(fieldType, contracts);
			}
		}
		else
		{
			Debug.Log("Unsupported type: " + fieldType.ToString());
		}

		return fieldContract;
	}
}

}
