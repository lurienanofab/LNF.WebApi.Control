using System;
using System.Configuration;
using System.Text;
using System.Web;
using System.Web.Http;
using System.Web.Http.Controllers;

namespace LNF.WebApi.Control
{
    public class BasicAuthenticationAttribute : AuthorizeAttribute
    {
        protected override bool IsAuthorized(HttpActionContext actionContext)
        {
            return CheckAuthorization(actionContext);
        }

        protected override void HandleUnauthorizedRequest(HttpActionContext actionContext)
        {
            // This will prevent redirecting to the login page, which is bad for webapi.
            if (HttpContext.Current != null)
                HttpContext.Current.Response.SuppressFormsAuthenticationRedirect = true;

            base.HandleUnauthorizedRequest(actionContext);
        }

        protected bool IsAuthenticated(HttpActionContext actionContext)
        {
            if (actionContext.RequestContext.Principal != null)
                if (actionContext.RequestContext.Principal.Identity != null)
                    return actionContext.RequestContext.Principal.Identity.IsAuthenticated;

            return false;
        }

        public bool CheckAuthorization(HttpActionContext actionContext)
        {
            if (actionContext.Request.Headers.Authorization != null)
            {
                if (actionContext.Request.Headers.Authorization.Scheme.ToLower() == "basic")
                {
                    string authenticationString = actionContext.Request.Headers.Authorization.Parameter;
                    string originalString = Encoding.UTF8.GetString(Convert.FromBase64String(authenticationString));

                    string[] splitter = originalString.Split(':');
                    string username = splitter[0];
                    string password = splitter[1];

                    if (ValidateUser(username, password))
                        return true;
                }
            }

            return false;
        }

        private bool ValidateUser(string username, string password)
        {
            string un = ConfigurationManager.AppSettings["BasicAuthUsername"];
            string pw = ConfigurationManager.AppSettings["BasicAuthPassword"];

            if (!string.IsNullOrEmpty(un) && !string.IsNullOrEmpty(pw))
            {
                if (username == un)
                {
                    if (password == pw)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}