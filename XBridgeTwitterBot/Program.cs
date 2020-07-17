using System;
using Tweetinvi;
using System.Threading.Tasks;
using System.Net.Http.Headers;
using Microsoft.Extensions.Configuration;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Hosting;
using Discord;
using Discord.WebSocket;
using XBridgeTwitterBot.Config;
using XBridgeTwitterBot.Services;
using XBridgeTwitterBot.Interfaces;

namespace XBridgeTwitterBot
{
    class Program
    {
        public static async Task Main(string[] args)
        {
            var host = new HostBuilder()
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config.SetBasePath(hostingContext.HostingEnvironment.ContentRootPath);
                    config.AddJsonFile("appsettings.json", true, true);
                    config.AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}.json");
                })
                .ConfigureServices((hostingContext, services) =>
                {
                    var apiEndpoints = new ApiEndpoints();
                    hostingContext.Configuration.GetSection("ApiEndpoints").Bind(apiEndpoints);

                    services.Configure<DiscordCredentials>(options =>
                        hostingContext.Configuration.GetSection("Discord").Bind(options));


                    services.AddHttpClient<IBlocknetApiService, BlocknetApiService>(client =>
                    {
                        client.BaseAddress = new Uri(apiEndpoints.Blocknet);
                        client.DefaultRequestHeaders
                            .Accept
                            .Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    });

                    services.AddHttpClient<IDxDataService, DxDataService>(client =>
                    {
                        client.BaseAddress = new Uri(apiEndpoints.Native);
                        client.DefaultRequestHeaders
                            .Accept
                            .Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    });

                    var twitterCredentials = hostingContext.Configuration.GetSection("Twitter").Get<TwitterCredentials>();
                    
                    Auth.SetUserCredentials(
                        twitterCredentials.ConsumerKey,
                        twitterCredentials.ConsumerSecret,
                        twitterCredentials.UserAccessToken,
                        twitterCredentials.UserAccessSecret
                    );

                    services.AddSingleton(new DiscordSocketClient(new DiscordSocketConfig
                    {                                       
                        LogLevel = LogSeverity.Verbose,     
                        MessageCacheSize = 1000             
                    }));

                    services.AddSingleton<StartupService>();
                    services.AddTransient<IComposeTweetService, ComposeTweetService>();

                    services.AddHostedService<TimedHostedService>();

                    services.AddLogging();
                })
                .Build();


            var provider = host.Services;

            await provider.GetRequiredService<StartupService>().StartAsync();

            await host.RunAsync();
        }      

    }
}