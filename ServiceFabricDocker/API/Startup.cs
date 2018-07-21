using System;
using System.Threading.Tasks;
using GrainInterfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;

namespace API
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();
            services.AddTransient<IClusterClient>(CreateClusterClient);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, Microsoft.AspNetCore.Hosting.IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseMvc();
            app.UseDefaultFiles();
            app.UseStaticFiles();
        }

        private IClusterClient CreateClusterClient(IServiceProvider serviceProvider)
        {
            var log = serviceProvider.GetService<ILogger<Startup>>();

            // TODO replace with your connection string
            const string connectionString = "your connection string";
            var client = new ClientBuilder()
                .ConfigureApplicationParts(parts => parts.AddApplicationPart(typeof(IValueGrain).Assembly))
                .Configure<ClusterOptions>(options =>
                {
                    options.ServiceId = "silo-dockerapi11";

                    // The cluster id identifies a deployed cluster. Since Service Fabric uses rolling upgrades, the cluster id
                    // can be kept constant. This is used to identify which silos belong to a particular cluster.
                    options.ClusterId = "development";
                })
                .UseAzureStorageClustering(options => options.ConnectionString = connectionString)
                .Build();
            int count = 0;
            client.Connect(RetryFilter).GetAwaiter().GetResult();
            return client;

            
            async Task<bool> RetryFilter(Exception exception)
            {
                log?.LogWarning("Exception while attempting to connect to Orleans cluster: {Exception}", exception);
                if (count == 5)
                    return false;
                await Task.Delay(TimeSpan.FromSeconds(2));
                ++count;
                return true;
            }
        }
    }
}
