using UnityEngine;
using System.Collections.Generic;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.TypeInspectors;
// Ew
using System.Linq;

namespace Cloverview
{

public sealed class UnitySerialisationTypeInspector : TypeInspectorSkeleton
{
	private readonly ITypeInspector innerTypeDescriptor;

	public UnitySerialisationTypeInspector(ITypeInspector innerTypeDescriptor)
	{
		this.innerTypeDescriptor = innerTypeDescriptor;
	}

	public override IEnumerable<IPropertyDescriptor> GetProperties(System.Type type, object container)
	{
		// return innerTypeDescriptor.GetProperties(type, container)
		// 	.Where(WouldBeSerialisedByUnity)
		// 	.OrderBy(DefaultOrdering);
		var properties = innerTypeDescriptor.GetProperties(type, container);
		properties = properties.Where(WouldBeSerialisedByUnity);
		return properties.OrderBy(DefaultOrdering);
	}

	static bool WouldBeSerialisedByUnity(IPropertyDescriptor p)
	{
		return p.Public || p.GetCustomAttribute<SerializeField>() != null;
	}

	static int DefaultOrdering(IPropertyDescriptor p)
	{
		return p.Order;
	}
}


}