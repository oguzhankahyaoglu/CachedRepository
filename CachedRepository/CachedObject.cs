using System;

namespace CachedRepository
{
    /// <summary>
    /// Runtime Cache'de kaydedilmek üzere, cached repo yazmak için kullanılabilr.
    /// Key'ler üzerinden ilgil entity'leri döner.
    /// CachedRepo'dan farklı olarak, bir koleksiyon değil, direkt olarak bir obje'yi cache'de tutar/yönetir
    /// </summary>
    /// <typeparam name="T">Repository'nin içerdiği entity tipi</typeparam>
    public abstract class CachedObject<T> : CachedRepoBase<T>
    {

        private T _GetCachedEntitiesFromCache() => GetFromCache(GetCacheKey());
        private void _SetCachedEntitiesToCache(T value) => SetCache(GetCacheKey(), value);

        public virtual T GetCachedEntities()
        {
            var result = _LazyCache.GetOrAdd(GetCacheKey(), () =>
            {
                try
                {
                    var cached = GetDataToBeCached();
                    LastCachedItemDate = DateTime.Now;
                    return cached;
                }
                catch (Exception e)
                {
                    throw new Exception($"{GetType().Name} repo'sundan data çekilirken hata oluştu", e);
                }
            }, CacheItemPolicyDefault);
            return result;
        }

        public virtual void SetCachedEntities(T value)
        {
            _SetCachedEntitiesToCache(value);
        }

        public override void ReleaseCache()
        {
            _SetCachedEntitiesToCache(default(T));
        }

        protected abstract T GetDataToBeCached();
    }
}
