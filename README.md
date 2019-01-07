
# CachedRepository
A caching infrastructure base for applications. built onto [LazyCache](https://www.nuget.org/packages?q=lazycache)  library.

 - Thread Safe (thanks to lazycache)
 - Data source is requested when needed, only once (lazy cache)
 - Supports multiple types for storing like dictionary, list or any object
 - Cleaner code with classes instead of cache read/write lines
 - Expiration is configurable per repository
 - It is very easy to make expiration conditionally (such as expiring a product object based on discount end date)
 - For most of the cases, implementation of the `GetDataToBeCached` method is enough to use.

![How CachedRepository works](https://cdn.okahyaoglu.net/cachedrepository.png)

## CachedObject
Stores only single object. 
Usage (storing whole dictionary which comes from a WCF service):

    public class GetProductSoftwareUpdatesRepo : CachedObject<Dictionary<string, string>>
    {
        protected override Dictionary<string, string> GetDataToBeCached()
        {
            var result = ServiceCaller.PMS(ws => ws.GetProductSoftwareUpdates());
            return result;
        }
    }

To use the repository (which fetches data from remote if cache is missed):

    var items = new GetProductSoftwareUpdatesRepo().GetCachedEntities();

## CachedRepo
Stores a list of same type, behaves like a `List<T>` . 
Usage (stores an enumeration of `GetHandlerOutboundUrlsAll_Result` instances):

    public class GetHandlerOutboundUrlsRepo : CachedRepo<GetHandlerOutboundUrlsAll_Result>
    {
        protected override IEnumerable<GetHandlerOutboundUrlsAll_Result> GetDataToBeCached()
        {
            using (var ent = new HandlerEntities())
            {
                var result = ent.GetHandlerOutboundUrlsAll(4071).ToList().Where(a => a.REDIRECTSTATUS.IsNullOrEmptyString()).ToList();
                return result;
            }
        }
    }
    
   To use the repository (which fetches data from remote if cache is missed):

    var routingUrls = new RedirectsRepo().GetCachedEntities();
 
## CachedDictionary
Behaves like a dictionary and cached objects are stored per key of this dictionary. For requesting same key, data is returned from the cache. 

Usage (stores `IEnumerable<CmsContent>` per a string key):

    public class CampaignDetailUxRepo : CachedDictionary<IEnumerable<CmsContent>,string>
    {
        protected override IEnumerable<CmsContent> GetDataToBeCached(string key)
        {
            var result = ServiceCaller.CMS(ws => ws.GetContentListByPath(Constants.FolderPaths.CampaingsDetail, Constants.Main.SiteID, key)).ToArray();
            return result;
        }
    }
    
  To use the repository (which fetches data from remote if cache is missed):

     var contentLists = new CampaignDetailUxRepo().Get("1234567");

To specify expiration of items for single key, `ShouldExpireEntityItem` method should be overridden:

        internal class GetCampaignDetailRepo : CachedDictionary<HopiCampaignDetail, string>
    {
        protected override HopiCampaignDetail GetDataToBeCached(string key)
        {
            var result = ServiceCaller.Basket(ws => ws.GetCampaignDetail(key));
            return result;
        }

        protected override bool ShouldExpireEntityItem(string key, HopiCampaignDetail outResult)
        {
            return base.ShouldExpireEntityItem(key, outResult);
        }
    }



## Common Methods of All Types
Setting the cache expiration date for a single repository:

        protected override DateTime GetCacheExpireDate()
        {
            return DateTime.Now.AddMinutes(1);
        }

GetCachedEntities: which fetches data from remote if the cache is missed, otherwise returns the cached items. **There is an exception  for CachedDictionary type**. It directly returns all cached data for already-requested keys; due to the fact that it requests data with a key via `Get` method.


