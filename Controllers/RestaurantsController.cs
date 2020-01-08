using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using tacitdemo.Models;
using tacitdemo.Services;

namespace tacitdemo.Controllers
{
    [CustomErrorFilter]
    public class RestaurantsController : ApiController
    {
        /// <summary>
        /// To get list of all menu items in the restaurant
        /// </summary>
        /// <param name="restaurantId"></param>
        /// <returns>IEnumerable<RestaurantMenu></returns>
        /// <response code="200">Ok</response>
        /// <response code="404">Not Found</response>
        [Route("api/restaurants/{restaurantId}")]
        [HttpGet]
        
        public IEnumerable<RestaurantMenu> Get(string restaurantId)
        {
            Webordering restaurant = new Webordering();
            IEnumerable<RestaurantMenu> restaurantMenu = restaurant.GetRestaurantMenus(restaurantId);
            if (restaurantMenu == null || restaurantMenu.Count() == 0)
            {
                string message = string.Format("No Menu items found for restaurant id = {0}", restaurantId);
                throw new Exception(message);
            }
            return restaurantMenu;
        }

        /// <summary>
        /// To lookup for menu items in the restaurant
        /// </summary>
        /// <param name="restaurantId"></param>
        /// <param name="searchText"></param>
        /// <returns>IEnumerable<RestaurantMenu></returns>
        /// <response code="200">Ok</response>
        /// <response code="404">Not Found</response>
        [Route("api/restaurants/{restaurantId}/searchByMenu/{searchText}")]
        [HttpGet]
        public IEnumerable<RestaurantMenu> Get(string restaurantId, string searchText)
        {
            Webordering restaurant = new Webordering();
            IEnumerable<RestaurantMenu> restaurantMenu = restaurant.GetRestaurantMenus(restaurantId,searchText);
            if (restaurantMenu == null || restaurantMenu.Count()==0)
            {
                string message = string.Format("No Menu items found for restaurant id = {0} for search value '{1}'", restaurantId, searchText);
                throw new Exception(message);
            }
            return restaurantMenu;
        }

    }
}
