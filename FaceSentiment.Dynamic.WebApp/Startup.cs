using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(FaceSentiment.Dynamic.WebApp.Startup))]
namespace FaceSentiment.Dynamic.WebApp
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
