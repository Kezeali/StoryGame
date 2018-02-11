using UnityEngine;
using System.Collections.Generic;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace NotABear
{

	public class DataItemConverter : IYamlTypeConverter
	{
		private Dictionary<string, IDataItem> dataItems = new Dictionary<string, IDataItem>();

		public void AddDataItemRange<T>(T[] items)
			where T : IDataItem
		{
			for (int i = 0; i < items.Length; ++i)
			{
				AddDataItem(items[i].name, items[i]);
			}
		}

		public void AddDataItem(string name, IDataItem item)
		{
			IDataItem existingValue;
			if (this.dataItems.TryGetValue(name, out existingValue))
			{
				if (existingValue.GetType() != item.GetType())
				{
					Debug.LogErrorFormat("Two data items of different types have the same name!! {0} ({1}, {2})", existingValue.GetType(), item.GetType(), name);
				}
				else
				{
					Debug.LogWarningFormat("Overwriting value for data item '{0}' ({1} -> {2})", name, existingValue, item);
				}
			}
			this.dataItems[name] = item;
		}
		
		public IDataItem GetItem(string name)
		{
			return this.dataItems[name];
		}

	#region IYamlTypeConverter
		public bool Accepts(System.Type type)
		{
			return type == typeof(IDataItem);
		}

		public object ReadYaml(IParser parser, System.Type type)
		{
			var name = ((Scalar)parser.Current).Value;
			parser.MoveNext();
			IDataItem value;
			if (!this.dataItems.TryGetValue(name, out value))
			{
				Debug.LogError("Missing data item with ID: " + name);
			}
			else if (!type.IsAssignableFrom(value.GetType()))
			{
				Debug.LogErrorFormat("Value for {0} has wrong type {1}", name, value.GetType());
			}
			return value;
		}

		public void WriteYaml(IEmitter emitter, object value, System.Type type)
		{
			var dataItem = (IDataItem)value;
			if (!this.dataItems.ContainsKey(dataItem.name))
			{
				Debug.LogWarning("Writing unknown data item: " + dataItem.name);
			}
			emitter.Emit(new Scalar(dataItem.name));
		}
	#endregion
	}


}