using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ZooKeeper.Mgt.Website.Common;
using ZooKeeper.Mgt.Website.Filter;
using ZookeeperClient;

namespace ZooKeeper.Mgt.Website
{
    public class Startup
    {
        ILoggerFactory _loggerFactory;
        public Startup(IConfiguration configuration, ILoggerFactory loggerFactory)
        {
            Configuration = configuration;
            this._loggerFactory = loggerFactory;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddLogging(builder =>
            {
                builder
                    .AddConfiguration(Configuration.GetSection("Logging"))
                    .AddFilter("Microsoft", LogLevel.Warning)
                    .AddConsole();
            });
            services.AddSingleton<IConfiguration>(Configuration);

            var path = $"/$${Configuration["RootNodeName"]}";
            var zkClient = new ZooKeeperClient(Configuration["ZooKeeperAddress"]);
            bool exist = zkClient.ExistsAsync(path).ConfigureAwait(false).GetAwaiter().GetResult();
            if (!exist) zkClient.CreatePersistentAsync(path, new ZNode { Key = Configuration["RootNodeName"], Value = "Root Node", Description = "" }).ConfigureAwait(false).GetAwaiter().GetResult();

            services.AddSingleton<IZooKeeperClient>(zkClient);
            services.AddMvc(options =>
            {
                options.Filters.Add(typeof(ExceptionFilter));
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseBrowserLink();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseStaticFiles();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
