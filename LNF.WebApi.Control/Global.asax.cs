using LNF.Impl.DependencyInjection.Web;
using LNF.Repository;
using System.Web.Http;

namespace LNF.WebApi.Control
{
    public class WebApiApplication : System.Web.HttpApplication
    {
        private IUnitOfWork _uow;

        protected void Application_Start()
        {
            ServiceProvider.Current = IOC.Resolver.GetInstance<ServiceProvider>();
            GlobalConfiguration.Configure(WebApiConfig.Register);
        }

        protected void Application_BeginRequest()
        {
            _uow = ServiceProvider.Current.DataAccess.StartUnitOfWork();   
        }

        protected void Application_EndRequest()
        {
            if (_uow != null)
                _uow.Dispose();
        }
    }
}
