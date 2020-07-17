﻿using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Tweetinvi;
using XBridgeTwitterBot.Config;
using XBridgeTwitterBot.Interfaces;

namespace XBridgeTwitterBot.Services
{
    public class TimedHostedService : IHostedService, IDisposable
    {
        const double interval = 1000 * 60 * 60 * 24;
        //const double interval = 5000;

        private System.Timers.Timer _timer;
        private readonly ILogger<TimedHostedService> _logger;
        private readonly IComposeTweetService _composeTweetService;
        private readonly DiscordSocketClient _discordSocketClient;
        IOptions<DiscordCredentials> _discordCredentials;

        public TimedHostedService(
            ILogger<TimedHostedService> logger, 
            IComposeTweetService composeTweetService,
            DiscordSocketClient discord, 
            IOptions<DiscordCredentials> discordCredentials
            )
        {
            _logger = logger;
            _composeTweetService = composeTweetService;
            _discordSocketClient = discord;
            _discordCredentials = discordCredentials;
        }

        public Task StartAsync(CancellationToken stoppingToken)
        {
            Console.WriteLine("Dx Bot Service running.");

            Task.Run(async () => await Publish());

            _timer = new System.Timers.Timer(interval);
            _timer.Elapsed += new ElapsedEventHandler(DoWork);
            _timer.Enabled = true;

            return Task.CompletedTask;
        }

        private async void DoWork(object sender, ElapsedEventArgs e)
        {
            try
            {
                await Publish();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw ex;
            }
        }

        private async Task Publish()
        {
            var mainTweet = await _composeTweetService.ComposeTotalVolumeTweet();
            if (!string.IsNullOrEmpty(mainTweet))
            {
                Console.WriteLine(mainTweet);
                var childrenTweets = await _composeTweetService.ComposeVolumePerCoinTweets();

                childrenTweets.ForEach(ct => Console.WriteLine(ct));

                var completedOrdersTweet = await _composeTweetService.ComposeCompletedOrderTweet();

                Console.WriteLine(completedOrdersTweet);

                var openOrdersTweet = await _composeTweetService.ComposeMoreDetailsTweet();


                Console.WriteLine(openOrdersTweet);

                //var parentTweet = Tweet.PublishTweet(mainTweet);

                //Tweetinvi.Models.ITweet prevTweet = parentTweet;
                //Tweetinvi.Models.ITweet currTweet;
                //foreach (var childTweet in childrenTweets)
                //{
                //    currTweet = Tweet.PublishTweetInReplyTo(childTweet, prevTweet);
                //    prevTweet = currTweet;
                //};

                //var completedOrdersPostedTweet = Tweet.PublishTweetInReplyTo(completedOrdersTweet, prevTweet);

                //Tweet.PublishTweetInReplyTo(openOrdersTweet, completedOrdersPostedTweet);

                //var discordChannel = _discordSocketClient.GetChannel(_discordCredentials.Value.ChannelId) as IMessageChannel;
                //await discordChannel.SendMessageAsync(parentTweet.Url);
            }

        }

        public Task StopAsync(CancellationToken stoppingToken)
        {
            Console.WriteLine("Timed Hosted Service is stopping.");

            _timer.Enabled = false;

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}