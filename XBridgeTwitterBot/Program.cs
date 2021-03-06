﻿using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Tweetinvi;
using XBridgeTwitterBot.Config;
using XBridgeTwitterBot.Interfaces;
using XBridgeTwitterBot.Services;

namespace XBridgeTwitterBot
{
    class Program
    {
        public static async Task Main(string[] args)
        {
            var host = new HostBuilder()
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    Console.WriteLine("env host: " + hostingContext.HostingEnvironment.EnvironmentName);
                    //hostingContext.HostingEnvironment.EnvironmentName = "Development";
                    config.SetBasePath(hostingContext.HostingEnvironment.ContentRootPath);
                    config.AddJsonFile("appsettings.json", false);
                    config.AddJsonFile($"appsettings.{hostingContext.HostingEnvironment.EnvironmentName}.json", true);

                    config.AddEnvironmentVariables();
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