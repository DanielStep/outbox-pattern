using CAP.Application;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Outbox.Application;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CAP.API
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
            services.AddControllers();

            var connectionString = Configuration.GetValue<string>("DbConnection");
            var serviceBusConnection = Configuration.GetValue<string>("ServiceBusConnection");

            services.AddDbContext<OutboxDbContext>(options => options.UseSqlServer(connectionString));
            services.AddScoped<IMessageRepository, MessageRepository>();


            services.AddSingleton(new ServiceBusClientSingleton(connectionString));
            services.AddScoped<IServiceBus, ServiceBus>();

            services.AddScoped<IOutboxMessageDispatcher, OutboxMessageDispatcher>();


            services.AddCap(x =>
            {
                x.UseEntityFramework<OutboxDbContext>();
                x.UseAzureServiceBus(opt =>
                {
                    opt.ConnectionString = serviceBusConnection;
                });
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
