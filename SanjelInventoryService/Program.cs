using System.Configuration;
using System.IO;
using MetaShare.Common.Core.Daos.SqlServer;
using MetaShare.Common.Core.Proxies;
using Microsoft.AspNetCore.Hosting;
using Sanjel.Common.Daos;
using Sanjel.Common.ApiServices;
using Sanjel.Common.Services;

using CommonDaoFactory = MetaShare.Common.ServiceModel.Dao.DaoFactory;
using CommonServiceFactory = MetaShare.Common.ServiceModel.Services.ServiceFactory;
//using Sanjel.Services.Interfaces;

//using RegisterSanjelDataDaos = Sesi.SanjelData.Daos.RegisterDaos;
//using RegisterSanjelDataServices = Sesi.SanjelData.Services.RegisterServices;

namespace SanjelInventoryService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CommonDaoFactory.Instance.Clear();
            RegisterCommonDaos.RegisterAll(CommonDaoFactory.Instance);
            Sanjel.EService.Daos.RegisterDaos.RegisterAll(CommonDaoFactory.Instance);

            CommonServiceFactory.Instance.Clear();
            RegisterCommonServices.RegisterAll(CommonServiceFactory.Instance);
            Sanjel.Services.RegisterServices.RegisterAll(CommonServiceFactory.Instance);

            RegisterApiProxies.RegisterProxies(ProxyFactory.Instance);
            //RegisterWcfProxies.RegisterProxies(ProxyFactory.Instance);

            string connectionString = ConfigurationManager.ConnectionStrings["SanjelData"].ConnectionString;
            MetaShare.Common.Core.Daos.DaoFactory.Instance.ConnectionStringBuilder = new MetaShare.Common.Core.Daos.ConnectionStringBuilder(connectionString, typeof(MetaShare.Common.Core.Daos.SqlContext)) { SqlDialectType = typeof(SqlServerDialect), SqlDialectVersionType = typeof(SqlServerDialectVersion) };
            Sesi.SanjelData.Daos.RegisterDaos.RegisterAll(MetaShare.Common.Core.Daos.DaoFactory.Instance.ConnectionStringBuilder.SqlDialectType, MetaShare.Common.Core.Daos.DaoFactory.Instance.ConnectionStringBuilder.SqlDialectVersionType);
            Sesi.SanjelData.Services.RegisterServices.RegisterAll();

            var host = new WebHostBuilder()
                .UseKestrel()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseIISIntegration()
                .UseStartup<Startup>()
                //.UseApplicationInsights()
                .Build();
            host.Run();
        }

        //public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
        //    WebHost.CreateDefaultBuilder(args)
        //        .UseStartup<Startup>();
    }
}
