using System;
using System.Net;
using System.Net.Http;
using System.Web.Http.Filters;

namespace tacitdemo.Services
{
    internal class CustomErrorFilterAttribute : ExceptionFilterAttribute
    {
        public override void OnException(HttpActionExecutedContext actionExecutedContext)
        {
            HttpResponseMessage response = null;
            if (actionExecutedContext.Exception.Message.Contains("No Menu items found"))
            {
                response = new HttpResponseMessage(HttpStatusCode.NotFound)
                {
                    Content = new StringContent(actionExecutedContext.Exception.Message),
                    ReasonPhrase = ""
                };
            }
            else
            {
                response = new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    Content = new StringContent("Internal Server Error. Please Contact Administrator."),
                    ReasonPhrase = ""
                };
            }
            actionExecutedContext.Response = response;
        }
    }
}