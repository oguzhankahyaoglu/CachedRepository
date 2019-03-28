using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Caching;
using System.Threading;
using LazyCache;
using CacheItemPriority = System.Web.Caching.CacheItemPriority;

namespace CachedRepository
{
    public abstract class CachedRepoBase
    {
        public static Func<DateTime> DefaultExpireDate = () => DateTime.Now.RoundUp(TimeSpan.FromDays(31));

        public static DateTime? LastCachedItemDate { get; protected set; } = null;

        /// <summary>
        /// aynı günün 23:59:00 da expire olması
        /// </summary>
        public static Func<DateTime> DefaultExpireDate1Day = () => DateTime.Now.AddMonths(1).RoundUp(TimeSpan.FromDays(1));

        public static Func<DateTime> DefaultExpireDate5Min = () => DateTime.Now.RoundUp(TimeSpan.FromMinutes(5));

        public static Func<DateTime> DefaultExpireDate10Min = () => DateTime.Now.RoundUp(TimeSpan.FromMinutes(10));

        public static Func<DateTime> DefaultExpireDate1Hour = () => DateTime.Now.RoundUp(TimeSpan.FromHours(1));

        public static Func<int, DateTime> DefaultExpireDateXHours = (hours) => DateTime.Now.AddHours(hours).RoundUp(TimeSpan.FromHours(1));

        /// <summary>
        /// Sadece Memorycache'i boşaltır, Runtimecache'i elle temizlemek gerekiyor.
        /// </summary>
        public static void ReleaseAllCaches()
        {
            locker.Wait();
            try
            {
                //HttpContext.Current.Cache.ClearAll();
                var cacheKeys = _LazyCache.ObjectCache.Select(kvp => kvp.Key).ToList();
                foreach (string cacheKey in cacheKeys)
                    _LazyCache.ObjectCache.Remove(cacheKey);
            }
            finally
            {
                locker.Release();
            }
        }

        protected static readonly SemaphoreSlim locker = new SemaphoreSlim(1, 1);
        protected static CachingService _LazyCache = new CachingService();
        protected static ObjectCache _ObjectCache => _LazyCache.ObjectCache;
        /// <summary>
        /// AbsoluteExpiration ile cache'e eklemek için.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        protected CacheEntryRemovedCallback CacheItemRemovedCallback = arguments =>
            Debug.WriteLine($"[CachedDataSourceBase] Cache ({arguments.Source}: +{arguments.CacheItem}) Removed: {arguments.RemovedReason}");
        protected CacheItemPolicy CacheItemPolicyDefault => new CacheItemPolicy
        {
            AbsoluteExpiration = GetCacheExpireDate(),
            //Priority = (System.Runtime.Caching.CacheItemPriority) CacheItemPriority.AboveNormal,
            RemovedCallback = CacheItemRemovedCallback
        };

        protected virtual DateTime GetCacheExpireDate()
        {
            var expiration = DefaultExpireDate();
            return expiration;
        }

        //protected T Clone<T>(T source)
        //{
        //    //PERFORMANS DAHA ÖNEMLİ; CACHE'DEN DÖNEN DEĞERLERİ MANİPÜLE ETMEMELİ KİMSE...
        //    return source;
        //    //if (source == null)
        //    //    return source;

        //    //T result;
        //    //if (!Debugger.IsAttached)
        //    //{
        //    //    result = source.DeepClone();
        //    //    return result;
        //    //}

        //    //var sw = new Stopwatch();
        //    //sw.Start();
        //    //result = source.DeepClone();
        //    //sw.Stop();
        //    ////IGNORE 
        //    ////if (sw.ElapsedMilliseconds < 10)
        //    ////    return result;
        //    //DebugLog($"CLONING took {sw.Elapsed} with result of {result}");
        //    //return result;
        //}

        protected void DebugLog(string msg)
        {
            Debug.WriteLine($"[CACHEDREPO-{GetType().Name}] {msg}");
        }

        public abstract void ReleaseCache();
    }

    public abstract class CachedRepoBase<T> : CachedRepoBase
    {
        protected CachedRepoBase()
        {

        }

        protected virtual string GetCacheKey()
        {
            return "CachedRepoBase-" + GetType().FullName;
        }

        protected CacheItemPriority DefaultCacheItemPriority = CacheItemPriority.Default;
        protected T GetFromCache(String key)
        {
            var cachedItem = _LazyCache.Get<T>(key);
            return cachedItem;
        }

        ///// <summary>
        ///// Cache'in dependent olması gereken bir mekanizma olursa, bu metod ezilerek ilgili dependency verilebilir.
        ///// Default olarak null döner dependency olarak.
        ///// </summary>
        ///// <returns></returns>
        //protected virtual CacheDependency GetCacheDependency()
        //{
        //    //var cacheDependency = new CacheDependency(null, new[] { GLOBAL_CACHE_KEY });
        //    return null;
        //}

        /// <summary>
        /// Runtime Cache'e yazmakla görevlidir. Default davranışı 15 gün cache'de kalacak ve NotRemovable olacak şekilde cache'lemektir.
        /// Davranışını değiştirmek için extend edilmelidir.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="value"></param>
        protected void SetCache(String key, T value)
        {
            if (value.Equals(default(T)))
                _LazyCache.Remove(key);
            else
            {
                //var cacheDependency = GetCacheDependency();
                DebugLog($"Set new data, Repo expire date: {CacheItemPolicyDefault.AbsoluteExpiration}");
                _LazyCache.Add(key, value, CacheItemPolicyDefault);
                //HttpRuntime.Cache.Add(key, value, cacheDependency, expiration, Cache.NoSlidingExpiration,
                //DefaultCacheItemPriority, CacheItemRemovedCallback);
            }
        }

    }
}
