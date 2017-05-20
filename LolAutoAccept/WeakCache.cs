using System;
using System.Collections.Concurrent;

namespace LolAutoAccept
{
	public class WeakCache<TKey, TValue> where TValue : class
	{
		private ConcurrentDictionary<TKey, WeakReference<TValue>> Cache { get; } =
			new ConcurrentDictionary<TKey, WeakReference<TValue>>();

		public TValue GetOrAdd(TKey key, Func<TKey, TValue> factory)
		{

			if (Cache.TryGetValue(key, out WeakReference<TValue> weakRef)
			    && weakRef.TryGetTarget(out TValue cached))
				return cached;

			var value = factory(key);
			Cache.AddOrUpdate(key, new WeakReference<TValue>(value),
				(s, reference) =>
				{
					reference.SetTarget(value);
					return reference;
				});
			return value;
		}
	}
}