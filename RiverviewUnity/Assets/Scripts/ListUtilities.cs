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
		int end = firstIndex + count;
		end = list.Count < end ? list.Count : end;
		if (match == null) {
				return end;
		}
		int excludedIndex = firstIndex;
		// Find the first item which should be excluded
		while (excludedIndex < end && !match(list[excludedIndex], fixedArgument)) excludedIndex++;
		if (excludedIndex >= end) {
			// Everything should be excluded
			return end;
		}
		int current = excludedIndex + 1;
		while (current < end)
		{
			// Look for the next item which needs to be kept
			while (current < end && match(list[current], fixedArgument)) current++;
			if (current < end)
			{
				// if an item was found swap it with the first excluded index and move the exclusion index back
				T temp = list[excludedIndex];
				list[excludedIndex++] = list[current];
				list[current] = temp;
				// continue from the next item
				++current;
			}
		}
		return excludedIndex;
	}

	public static void Push<T>(this List<T> list, T value)
	{
		list.Add(value);
	}

	public static T Pop<T>(this List<T> list)
	{
		T value = default(T);
		if (list.Count > 0)
		{
			value = list[list.Count-1];
			list.RemoveAt(list.Count-1);
		}
		return value;
	}

	public static T Peek<T>(this List<T> list)
	{
		return list[list.Count-1];
	}

	public static T Peek<T>(this List<T> list, int i)
	{
		return list[list.Count-(i+1)];
	}

	public static void Enqueue< T>(this List<T> list, T value)
	{
		list.Add(value);
	}

	public static T Dequeue<T>(this List<T> list)
	{
		T value = default(T);
		if (list.Count > 0)
		{
			value = list[0];
			list.RemoveAt(0);
		}
		return value;
	}
}
