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
		return innerTypeDescriptor.GetProperties(type, container)
			.Where(p => p.GetCustomAttribute<SerializeField>() != null)
			// .Select(p =>
			// {
			// 	var descriptor = new PropertyDescriptor(p);
			// 	var member = p.GetCustomAttribute<YamlMemberAttribute>();
			// 	if (member != null)
			// 	{
			// 		if (member.SerializeAs != null)
			// 		{
			// 			descriptor.TypeOverride = member.SerializeAs;
			// 		}

			// 		descriptor.Order = member.Order;
			// 		descriptor.ScalarStyle = member.ScalarStyle;

			// 		if (member.Alias != null)
			// 		{
			// 			descriptor.Name = member.Alias;
			// 		}
			// 	}

			// 	return (IPropertyDescriptor)descriptor;
			// })
			.OrderBy(p => p.Order);
	}
}


}