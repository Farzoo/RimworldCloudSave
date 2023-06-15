using MemoryCacheT;

namespace RimworldCloudSave;

public static class CacheExtensions
{
    public static bool TryRemoveAndAdd<TKey, TValue>(this Cache<TKey, TValue> cache, TKey key, TValue value)
    {
        var flag = cache.Remove(key);
        return cache.TryAdd(key, value) && flag;
    }
    
    public static bool TryRemoveAndAdd<TKey, TValue>(this Cache<TKey, TValue> cache, TKey key, ICacheItem<TValue> cacheItem)
    {
        var flag = cache.Remove(key);
        return cache.TryAdd(key, cacheItem) && flag;
    }
    
}