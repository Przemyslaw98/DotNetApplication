using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(DotNetApplication.Startup))]
namespace DotNetApplication
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
