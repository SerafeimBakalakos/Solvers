namespace MGroup.Solvers.Commons
{
	using System.Collections.Generic;

	internal static class Utilities
    {
        internal static bool AreEqual(int[] array1, int[] array2)
        {
            if (array1.Length != array2.Length) return false;
            for (int i = 0; i < array1.Length; ++i)
            {
                if (array1[i] != array2[i]) return false;
            }
            return true;
        }

		internal static bool DictionariesHaveSameKeys<TKey, TValue>(
			IReadOnlyDictionary<TKey, TValue> dict1, IReadOnlyDictionary<TKey, TValue> dict2)
		{
			if (dict1.Count != dict2.Count)
			{
				return false;
			}

			foreach (TKey key in dict1.Keys)
			{
				if (!dict2.ContainsKey(key))
				{
					return false;
				}
			}

			return true;
		}
	}
}
