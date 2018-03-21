using UnityEngine;
using System.Collections.Generic;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Cloverview
{

	public sealed class DataItemConverter : IYamlTypeConverter
	{
		private class DataItemCollection : Dictionary<string, IDataItem>
		{
		}
		private Dictionary<System.Type, DataItemCollection> dataItemsCollections = new Dictionary<System.Type, DataItemCollection>();

		public void AddDataItemRange<T>(T[] items)
			where T : IDataItem
		{
			for (int i = 0; i < items.Length; ++i)
			{
				AddDataItem(items[i].name, items[i], typeof(T));
			}
		}

		public void AddDataItem(string name, IDataItem item, System.Type typeHint = null)
		{
			typeHint = typeHint ?? typeof(IDataItem);

			DataItemCollection dataItems = null;
			if (!this.dataItemsCollections.TryGetValue(typeHint, out dataItems))
			{
				dataItems = new DataItemCollection();
				this.dataItemsCollections[typeHint] = dataItems;
			}
		#if DEVELOPMENT_BUILD
			IDataItem existingValue;
			if (dataItems.TryGetValue(name, out existingValue))
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
		#endif
			dataItems[name] = item;
		}
		
		public IDataItem GetItem(string name, System.Type type)
		{
			IDataItem result = null;

			type = type ?? typeof(IDataItem);

			DataItemCollection dataItems = null;
			if (this.dataItemsCollections.TryGetValue(type, out dataItems))
			{
				result = dataItems[name];
			}
			return result;
		}

	#region IYamlTypeConverter
		public bool Accepts(System.Type type)
		{
			//return this.dataItemsCollections.ContainsKey(type);
			return typeof(IDataItem).IsAssignableFrom(type);
		}

		public object ReadYaml(IParser parser, System.Type type)
		{
			var name = ((Scalar)parser.Current).Value;
			parser.MoveNext();

			type = type ?? typeof(IDataItem);

			DataItemCollection dataItems = null;
			this.dataItemsCollections.TryGetValue(type, out dataItems);

			IDataItem value = null;
			if (dataItems != null)
			{
				if (name == "null" || name.Length == 0)
				{
					// All good.
				}
				else if (!dataItems.TryGetValue(name, out value))
				{
					Debug.LogError("Missing data item with ID: " + name);
				}
				else if (!type.IsAssignableFrom(value.GetType()))
				{
					Debug.LogErrorFormat("Value for {0} has wrong type {1}", name, value.GetType());
				}
			}
			return value;
		}

		public void WriteYaml(IEmitter emitter, object value, System.Type type)
		{
			var dataItem = value as IDataItem;
		#if DEVELOPMENT_BUILD
			if (dataItem != null)
			{
				type = type ?? typeof(IDataItem);
				DataItemCollection dataItems = null;
				this.dataItemsCollections.TryGetValue(type, out dataItems);
				if (dataItems == null || !dataItems.ContainsKey(dataItem.name))
				{
					Debug.LogWarning("Writing unknown data item: " + dataItem.name);
				}
			}
		#endif
			if (dataItem != null)
			{
				emitter.Emit(new Scalar(dataItem.name ?? "missing"));
			}
			else
			{
				emitter.Emit(new Scalar("tag:yaml.org,2002:null", "null"));
			}
		}
	#endregion
	}


}