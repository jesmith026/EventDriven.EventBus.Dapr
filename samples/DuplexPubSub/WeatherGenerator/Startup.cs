using EventDriven.EventBus.Dapr;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WeatherGenerator.Factories;
using WeatherGenerator.Handlers;

namespace WeatherGenerator
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
            // Add factories
            services.AddSingleton<WeatherFactory>();

            // Add handlers
            services.AddSingleton<WeatherForecastRequestedEventHandler>();

            // Configuration
            var eventBusOptions = new DaprEventBusOptions();
            Configuration.GetSection(nameof(DaprEventBusOptions)).Bind(eventBusOptions);
            var eventBusSchemaOptions = new DaprEventBusSchemaOptions();
            Configuration.GetSection(nameof(DaprEventBusSchemaOptions)).Bind(eventBusSchemaOptions);

            // Add Dapr service bus
            services.AddDaprEventBus(eventBusOptions.PubSubName, options =>
            {
                options.UseSchemaRegistry = eventBusSchemaOptions.UseSchemaRegistry;
                options.SchemaRegistryType = eventBusSchemaOptions.SchemaRegistryType;
                options.MongoStateStoreOptions = eventBusSchemaOptions.MongoStateStoreOptions;
                options.SchemaValidatorType = eventBusSchemaOptions.SchemaValidatorType;
                options.AddSchemaOnPublish = eventBusSchemaOptions.AddSchemaOnPublish;
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env,
            WeatherForecastRequestedEventHandler forecastRequestedEventHandler)
        {
            app.UseRouting();
            app.UseCloudEvents();
            app.UseEndpoints(endpoints =>
            {
                // Map SubscribeHandler and DaprEventBus
                endpoints.MapSubscribeHandler();
                endpoints.MapDaprEventBus(eventBus =>
                {
                    // Subscribe with a handler
                    eventBus.Subscribe(forecastRequestedEventHandler, null, "v1");
                });
            });
        }
    }
}
