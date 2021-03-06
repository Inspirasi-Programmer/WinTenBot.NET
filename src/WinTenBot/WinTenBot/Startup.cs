using System;
using Hangfire;
using Hangfire.Heartbeat;
using Hangfire.Heartbeat.Server;
using Hangfire.LiteDB;
using HangfireBasicAuthenticationFilter;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Telegram.Bot;
using Telegram.Bot.Framework;
using Telegram.Bot.Framework.Abstractions;
using WinTenBot.Bots;
using WinTenBot.Extensions;
using WinTenBot.Handlers;
using WinTenBot.Handlers.Commands.Additional;
using WinTenBot.Handlers.Commands.Chat;
using WinTenBot.Handlers.Commands.Core;
using WinTenBot.Handlers.Commands.Group;
using WinTenBot.Handlers.Commands.Notes;
using WinTenBot.Handlers.Commands.Rss;
using WinTenBot.Handlers.Commands.Rules;
using WinTenBot.Handlers.Commands.Security;
using WinTenBot.Handlers.Commands.Tags;
using WinTenBot.Handlers.Commands.Welcome;
using WinTenBot.Handlers.Events;
using WinTenBot.Helpers;
using WinTenBot.Interfaces;
using WinTenBot.Model;
using WinTenBot.Options;
using WinTenBot.Scheduler;
using WinTenBot.Services;

namespace WinTenBot
{
    public class Startup
    {
        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;

            GlobalConfiguration.Configuration
                .UseSerilogLogProvider()
                .UseColouredConsoleLogProvider();

            Bot.GlobalConfiguration = Configuration;
            Bot.DbConnectionString = Configuration["CommonConfig:ConnectionString"];

            Console.WriteLine($"ProductName: {Configuration["Engines:ProductName"]}");
            Console.WriteLine($"Version: {Configuration["Engines:Version"]}");

            Bot.Client = new TelegramBotClient(Configuration["ZiziBot:ApiToken"]);
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddTransient<ZiziBot>()
                .Configure<BotOptions<ZiziBot>>(Configuration.GetSection("ZiziBot"))
                .Configure<CustomBotOptions<ZiziBot>>(Configuration.GetSection("ZiziBot"))
                
                .AddTransient<MacOsBot>()
                .Configure<BotOptions<MacOsBot>>(Configuration.GetSection("MacOsBot"))
                .Configure<CustomBotOptions<MacOsBot>>(Configuration.GetSection("MacOsBot"))
                
                .AddScoped<NewUpdateHandler>()
                .AddScoped<GenericMessageHandler>()
                .AddScoped<WebhookLogger>()
                .AddScoped<StickerHandler>()
                .AddScoped<WeatherReporter>()
                .AddScoped<ExceptionHandler>()
                .AddScoped<UpdateMembersList>()
                .AddScoped<CallbackQueryHandler>()
                .AddScoped<IWeatherService, WeatherService>();

            services.AddScoped<GlobalBanCommand>()
                .AddScoped<DelBanCommand>();

            services.AddScoped<WordFilterCommand>()
                .AddScoped<WordSyncCommand>();

            services.AddScoped<PingHandler>()
                .AddScoped<HelpCommand>()
                .AddScoped<TestCommand>();

            services.AddScoped<MediaReceivedHandler>();

            services.AddScoped<MigrateCommand>()
                .AddScoped<MediaFilterCommand>();

            services.AddScoped<CallTagsReceivedHandler>()
                .AddScoped<TagsCommand>()
                .AddScoped<TagCommand>()
                .AddScoped<UntagCommand>();

            services.AddScoped<NotesCommand>()
                .AddScoped<AddNotesCommand>();

            services.AddScoped<SetRssCommand>()
                .AddScoped<DelRssCommand>()
                .AddScoped<RssInfoCommand>()
                .AddScoped<RssPullCommand>()
                .AddScoped<RssCtlCommand>();

            services.AddScoped<AdminCommand>()
                .AddScoped<PinCommand>()
                .AddScoped<ReportCommand>()
                .AddScoped<AfkCommand>();

            services.AddScoped<KickCommand>()
                .AddScoped<BanCommand>();

            services.AddScoped<PromoteCommand>()
                .AddScoped<DemoteCommand>();

            services.AddScoped<RulesCommand>();

            services.AddScoped<NewChatMembersEvent>()
                .AddScoped<LeftChatMemberEvent>()
                .AddScoped<PinnedMessageEvent>();

            services.AddScoped<WelcomeCommand>();
            services.AddScoped<SetWelcomeCommand>();

            services.AddScoped<PingCommand>()
                .AddScoped<DebugCommand>()
                .AddScoped<StartCommand>()
                .AddScoped<IdCommand>()
                .AddScoped<InfoCommand>();

            services.AddScoped<OutCommand>();

            services.AddScoped<QrCommand>();


            // Hangfire
            var hangfireOptions = new LiteDbStorageOptions
            {
                QueuePollInterval = TimeSpan.FromSeconds(5)
            };

            services.AddHangfireServer();
            services.AddHangfire(t =>
            {
                t.UseLiteDbStorage(Configuration["Hangfire:LiteDb"], hangfireOptions);
                t.UseHeartbeatPage(checkInterval: TimeSpan.FromSeconds(1));
                t.UseSerilogLogProvider();
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            Bot.HostingEnvironment = env;

            var hangfireBaseUrl = Configuration["Hangfire:BaseUrl"];
            var hangfireUsername = Configuration["Hangfire:Username"];
            var hangfirePassword = Configuration["Hangfire:Password"];

            ConsoleHelper.WriteLine($"Hangfire Auth: {hangfireUsername} | {hangfirePassword}");

            var options = new DashboardOptions
            {
                Authorization = new[]
                {
                    new HangfireCustomBasicAuthenticationFilter {User = hangfireUsername, Pass = hangfirePassword}
                }
            };

            var configureBot = ConfigureBot();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();

                // get bot updates from Telegram via long-polling approach during development
                // this will disable Telegram webhooks
                app.UseTelegramBotLongPolling<ZiziBot>(configureBot, TimeSpan.FromSeconds(1));
                app.UseTelegramBotLongPolling<MacOsBot>(configureBot, TimeSpan.FromSeconds(1));

                app.UseHangfireDashboard("/hangfire", options);
            }
            else
            {
                // use Telegram bot webhook middleware in higher environments
                app.UseTelegramBotWebhook<ZiziBot>(configureBot);
                app.UseTelegramBotWebhook<MacOsBot>(configureBot);

                // and make sure webhook is enabled
                app.EnsureWebhookSet<ZiziBot>();
                app.EnsureWebhookSet<MacOsBot>();

                app.UseHangfireDashboard(hangfireBaseUrl, options);
            }

            app.UseHangfireServer(additionalProcesses: new[]
            {
                new ProcessMonitor(checkInterval: TimeSpan.FromSeconds(1))
            });

            app.Run(async context => { await context.Response.WriteAsync("Hello World!"); });

            BotScheduler.StartScheduler();

            ConsoleHelper.WriteLine("App is ready.");
        }

        private IBotBuilder ConfigureBot()
        {
            return new BotBuilder()
                    .Use<ExceptionHandler>()
                    // .Use<CustomUpdateLogger>()
                    .UseWhen<WebhookLogger>(When.Webhook)
                    
                    .UseWhen<NewUpdateHandler>(When.NewUpdate)
                    
                    //.UseWhen<UpdateMembersList>(When.MembersChanged)
                    .UseWhen<NewChatMembersEvent>(When.NewChatMembers)
                    .UseWhen<LeftChatMemberEvent>(When.LeftChatMember)

                    //.UseWhen(When.MembersChanged, memberChanged => memberChanged
                    //    .UseWhen(When.MembersChanged, cmdBranch => cmdBranch
                    //        .Use<NewChatMembersCommand>()
                    //        )
                    //    )
                    .UseWhen<PinnedMessageEvent>(When.NewPinnedMessage)
                    .UseWhen<MediaReceivedHandler>(When.MediaReceived)
                    .UseWhen(When.NewMessage, msgBranch => msgBranch
                        .UseWhen(When.NewTextMessage, txtBranch => txtBranch
                                .UseWhen<CallTagsReceivedHandler>(When.CallTagReceived)
                                .UseWhen<PingHandler>(When.PingReceived)
                                .UseWhen(When.NewCommand, cmdBranch => cmdBranch
                                    .UseCommand<AdminCommand>("admin")
                                    .UseCommand<AddNotesCommand>("addfilter")
                                    .UseCommand<AfkCommand>("afk")
                                    .UseCommand<BanCommand>("ban")
                                    .UseCommand<DebugCommand>("dbg")
                                    .UseCommand<DelBanCommand>("dban")
                                    .UseCommand<DelRssCommand>("delrss")
                                    .UseCommand<DemoteCommand>("demote")
                                    .UseCommand<GlobalBanCommand>("gban")
                                    .UseCommand<HelpCommand>("help")
                                    .UseCommand<IdCommand>("id")
                                    .UseCommand<InfoCommand>("info")
                                    .UseCommand<KickCommand>("kick")
                                    .UseCommand<MediaFilterCommand>("mfil")
                                    .UseCommand<MigrateCommand>("migrate")
                                    .UseCommand<NotesCommand>("filters")
                                    .UseCommand<OutCommand>("out")
                                    .UseCommand<PinCommand>("pin")
                                    .UseCommand<PingCommand>("ping")
                                    .UseCommand<PromoteCommand>("promote")
                                    .UseCommand<QrCommand>("qr")
                                    .UseCommand<ReportCommand>("report")
                                    .UseCommand<RssCtlCommand>("rssctl")
                                    .UseCommand<RssInfoCommand>("rssinfo")
                                    .UseCommand<RssPullCommand>("rsspull")
                                    .UseCommand<RulesCommand>("rules")
                                    .UseCommand<SetRssCommand>("setrss")
                                    .UseCommand<SetWelcomeCommand>("setwelcome")
                                    .UseCommand<StartCommand>("start")
                                    .UseCommand<TagCommand>("tag")
                                    .UseCommand<TagsCommand>("tags")
                                    .UseCommand<TestCommand>("test")
                                    .UseCommand<UntagCommand>("untag")
                                    .UseCommand<WelcomeCommand>("welcome")
                                    .UseCommand<WordFilterCommand>("wfil")
                                    .UseCommand<WordSyncCommand>("wsync")
                                )
                                .Use<GenericMessageHandler>()

                            //.Use<NLP>()
                        )
                        // .UseWhen<StickerHandler>(When.StickerMessage)
                        .UseWhen<WeatherReporter>(When.LocationMessage)
                    )
                    .UseWhen<CallbackQueryHandler>(When.CallbackQuery)

                //.Use<UnhandledUpdateReporter>()
                ;
        }
    }
}