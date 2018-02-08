using UnityEngine;
using System.Collections.Generic;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace NotABear
{

	public class DataItemConverter : IYamlTypeConverter
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

	#region IYamlTypeConverter
		public bool Accepts(System.Type type)
		{
			return type == typeof(DataItem);
		}

		public object ReadYaml(IParser parser, System.Type type)
		{
			var name = ((Scalar)parser.Current).Value;
			parser.MoveNext();
			DataItem value;
			if (!dataItems.TryGetValue(name, out value))
			{
				Debug.LogError("Missing data item with ID: " + name);
			}
			return value;
		}

		public void WriteYaml(IEmitter emitter, object value, System.Type type)
		{
			var dataItem = (DataItem)value;
			if (!dataItems.ContainsKey(dataItem.name))
			{
				Debug.LogWarning("Writing unknown data item: " + dataItem.name);
			}
			emitter.Emit(new Scalar(dataItem.name));
		}
	#endregion
	}


}