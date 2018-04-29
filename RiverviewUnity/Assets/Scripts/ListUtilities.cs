using UnityEngine;
using System.Collections.Generic;

public static class ListUtilities
{
	public static int RemoveAll<T, P1>(this List<T> list, System.Func<T, P1, bool> match, P1 fixedArgument)
	{
		int size = list.Count;
		int freeIndex = ListUtilities.ExcludeAll(list, match, fixedArgument);
		list.RemoveRange(freeIndex, size - freeIndex);
		int result = size - freeIndex;
		return result;
	}

	public static int ExcludeAll<T, P1>(this List<T> list, System.Func<T, P1, bool> match, P1 fixedArgument)
	{
		return ListUtilities.ExcludeAll(list, match, fixedArgument, 0, list.Count);
	}

	public static int ExcludeAll<T, P1>(this List<T> list, System.Func<T, P1, bool> match, P1 fixedArgument, int firstIndex, int count)
	{
		if (match == null) {
				return firstIndex + count;
		}
		int freeIndex = firstIndex;   // the first free slot in items array
		int maxSize = (list.Count-firstIndex);
		int size = maxSize < count ? maxSize : count;
		// Find the first item which needs to be removed.
		while (freeIndex < size && !match(list[freeIndex], fixedArgument)) freeIndex++;
		if (freeIndex >= size) return firstIndex + count;
		int current = freeIndex + 1;
		while (current < size)
		{
			// Find the first item which needs to be kept.
			while (current < size && match(list[current], fixedArgument)) current++;
			if( current < size)
			{
				// copy item to the free slot.
				list[freeIndex++] = list[current++];
			}
		}
		return freeIndex;
	}
}
