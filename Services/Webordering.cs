using ServiceStack.Redis;
using ServiceStack.Redis.Generic;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Web.Script.Serialization;
using tacitdemo.Models;

namespace tacitdemo.Services
{
    public class Webordering
    {
        #region "Constants"
        const string CacheConnectionString = "CacheConnectionString";
        const string CacheKeyPrefix = "CacheKeyPrefix";
        const string tacitApiUrl = "tacitApiUrl";
        const string Authorization = "Authorization";
        const string SiteName = "Site-Name";
        const string AppKey = "App-Key";
        const string AppLanguage = "App-Language";

        const string Id = "Id";
        const string Name = "Name";
        const string DeliveryTypeCode = "DeliveryTypeCode";
        const string MenuItems = "MenuItems";
        const string MenuItemGroups = "MenuItemGroups";
        const string RestaurantMenus = "RestaurantMenus";
        #endregion

        /// <summary>
        /// To get list of menuitems in the Restaurant 
        /// </summary>
        /// <param name="restaurantId"></param>
        /// <param name="searchText"></param>
        /// <returns>IEnumerable<RestaurantMenu></returns>
        public IEnumerable<RestaurantMenu> GetRestaurantMenus(string restaurantId, string searchText=null)
        {
            IEnumerable<RestaurantMenu> searchList = null;
            RedisClient redisClient = new RedisClient(ConfigurationManager.AppSettings[CacheConnectionString]);
            if (redisClient != null && !String.IsNullOrEmpty(restaurantId))
            {
                //Read Redis Cache
                IRedisTypedClient<RestaurantMenu> redis = redisClient.As<RestaurantMenu>();
                IRedisList<RestaurantMenu> menuList = redis.Lists[ConfigurationManager.AppSettings[CacheKeyPrefix]];

                //Load Redis Cache
                if (menuList == null || menuList.Count == 0)
                {
                    menuList = LoadMenuToRedisCache(restaurantId);
                }

                //Search cache for menuitem and return search list                
                if (menuList != null)
                {
                    if (!String.IsNullOrEmpty(searchText))
                    {
                        searchList = menuList.Where<RestaurantMenu>(menuitem => menuitem.MenuItemName.Contains(searchText.ToUpper()));
                    }
                    else
                    {
                        searchList = menuList.AsEnumerable<RestaurantMenu>();
                    }
                }
            }
            return searchList;
        }

        /// <summary>
        /// to load menuitems to the Redis Cache
        /// </summary>
        /// <param name="restaurantId"></param>
        /// <returns>IRedisList<RestaurantMenu></returns>
        private IRedisList<RestaurantMenu> LoadMenuToRedisCache(string restaurantId)
        {
            IRedisList<RestaurantMenu> menuList = null;
            //Get Restaurant Menus 
            Dictionary<string, object> restaurantData = GetWebResponse(ConfigurationManager.AppSettings[tacitApiUrl] + "restaurants/" + restaurantId );
            if (!String.IsNullOrEmpty(restaurantId))
            {
                object[] restaurantMenus = (object[])restaurantData[RestaurantMenus];
                Dictionary<string, object> menuData = null;

                if (restaurantMenus != null)
                {
                    RestaurantMenu menuCache = null;
                    
                    //Create Redis cache object
                    //var redis = ConnectionMultiplexer.Connect("tacitdevpingcache.redis.cache.windows.net:6379,password=Ws6YOzvK9SjEB/eUTPBfA1K02+VatGb98+9u+fLMem0=,name=tltest,allowAdmin=false,abortConnect=false,connectTimeout=30000,syncTimeout=30000,responseTimeout=30000,defaultDatabase=100");
                    RedisClient redisClient = new RedisClient(ConfigurationManager.AppSettings[CacheConnectionString]);
                    redisClient.Remove(ConfigurationManager.AppSettings[CacheKeyPrefix]);
                    IRedisTypedClient<RestaurantMenu> redis = redisClient.As<RestaurantMenu>();
                    menuList = redis.Lists[ConfigurationManager.AppSettings[CacheKeyPrefix]];

                    for (int index = 0; index < restaurantMenus.Length; index++)
                    {
                        Dictionary<string, object> menu = (Dictionary<string, object>)restaurantMenus[index];
                        String menuId = menu[Id].ToString();
                        String menuName = menu[Name].ToString();
                        String deliveryType = menu[DeliveryTypeCode].ToString();

                        //Get MenuItems from each Menu
                        if (!String.IsNullOrEmpty(menuId))
                        {
                            menuData = GetWebResponse(ConfigurationManager.AppSettings[tacitApiUrl] + "menus/" + menuId.Trim());
                            object[] menuItemGroups = (object[])menuData[MenuItemGroups];

                            for (int group = 0; group < menuItemGroups.Length; group++)
                            {
                                Dictionary<string, object> menuItemGroup = (Dictionary<string, object>)menuItemGroups[group];
                                object[] menuItems = (object[])menuItemGroup[MenuItems];
                                String menuGroupName = menuItemGroup[Name].ToString();
                                
                                if (menuItems != null)
                                {
                                    for (int item = 0; item < menuItems.Length; item++)
                                    {
                                        Dictionary<string, object> menuItem = (Dictionary<string, object>)menuItems[item];
                                        String menuItemId = menuItem[Id].ToString();
                                        String menuItemName = menuItem[Name].ToString();

                                        //Add menuitems to Redis Cache
                                        using (redisClient)
                                        {                  
                                            menuCache = new RestaurantMenu
                                            {
                                                MenuItemId = menuItemId,
                                                MenuItemName = menuItemName,
                                                MenuId = menuId,
                                                MenuName = menuName,
                                                DeliveryTypeCode = deliveryType
                                            };

                                            menuList.Add(menuCache);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return menuList;
        }

        /// <summary>
        /// to get json response from Tacit webapi
        /// </summary>
        /// <param name="url"></param>
        /// <returns>Dictionary<string, object></returns>
        private Dictionary<string, object> GetWebResponse(String url)
        {
            Dictionary<string, object> jsonData = null;
            HttpWebRequest webreq = (HttpWebRequest)WebRequest.Create(url);

            webreq.ContentType = "application/json";
            webreq.Headers.Add(Authorization, ConfigurationManager.AppSettings[Authorization]);
            webreq.Headers.Add(SiteName, ConfigurationManager.AppSettings[SiteName]);
            webreq.Headers.Add(AppKey, ConfigurationManager.AppSettings[AppKey]);
            webreq.Headers.Add(AppLanguage, ConfigurationManager.AppSettings[AppLanguage]);

            HttpWebResponse webres = (HttpWebResponse)webreq.GetResponse();

            Stream resStream = webres.GetResponseStream();
            using (StreamReader resReader = new StreamReader(resStream, true))
            {
                String jsonString = resReader.ReadToEnd();
                JavaScriptSerializer serializer = new JavaScriptSerializer();
                jsonData = (Dictionary<string, object>)serializer.DeserializeObject(jsonString);
            }
            return jsonData;
        }
    }
}