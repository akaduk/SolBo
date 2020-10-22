﻿using McMaster.NETCore.Plugins;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using Quartz;
using Quartz.Impl;
using SolBo.Agent.DI;
using SolBo.Agent.Factories;
using SolBo.Shared.Domain.Configs;
using SolBo.Shared.Domain.Enums;
using SolBo.Shared.Strategies;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Permissions;
using System.Threading;
using System.Threading.Tasks;

namespace SolBo.Agent
{
    class Program
    {
        private static readonly string appId = "solbo-runtime";

        private static ISchedulerFactory _schedulerFactory;
        private static IScheduler _scheduler;

        private static readonly Logger Logger = LogManager.GetLogger("SOLBO");

        [SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.ControlAppDomain)]
        static async Task<int> Main()
        {
            var cancellationTokenSource = new CancellationTokenSource();

            AppDomain currentDomain = AppDomain.CurrentDomain;
            currentDomain.UnhandledException += new UnhandledExceptionEventHandler(UnhandledExceptionHandler);

            AppDomain.CurrentDomain.ProcessExit += (s, e) => cancellationTokenSource.Cancel();
            Console.CancelKeyPress += (s, e) => cancellationTokenSource.Cancel();

            LogManager.Configuration.Variables["fileName"] = $"{appId}-{DateTime.UtcNow:ddMMyyyy}.log";
            LogManager.Configuration.Variables["archiveFileName"] = $"{appId}-{DateTime.UtcNow:ddMMyyyy}.log";

            var cfgBuilder = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile($"appsettings.{appId}.json");

            var configuration = cfgBuilder.Build();

            var app = configuration.Get<App>();

            try
            {
                var loaders = GetPluginLoaders();

                var servicesProvider = DependencyProvider.Get(app, loaders);

                var jobFactory = new JobFactory(servicesProvider);

                _schedulerFactory = new StdSchedulerFactory();

                _scheduler = await _schedulerFactory.GetScheduler();

                _scheduler.JobFactory = jobFactory;

                await _scheduler.Start();

                foreach (var loader in loaders)
                {
                    foreach (var pluginType in loader
                        .LoadDefaultAssembly()
                        .GetTypes()
                        .Where(t => typeof(IStrategy).IsAssignableFrom(t) && !t.IsAbstract))
                    {
                        var plugin = Activator.CreateInstance(pluginType) as IStrategy;

                        var runtimeJob = plugin?.StrategyRuntime();

                        var job = app.Strategies.FirstOrDefault(s => s.Name == plugin?.Name());

                        if(!(job is null))
                        {
                            switch (job.IntervalType)
                            {
                                case IntervalType.SECONDS:
                                    {
                                        runtimeJob.Item2.WithSimpleSchedule(x => x
                                            .WithIntervalInSeconds(job.Interval)
                                            .RepeatForever());
                                    }
                                    break;
                                case IntervalType.MINUTES:
                                    {
                                        runtimeJob.Item2.WithSimpleSchedule(x => x
                                            .WithIntervalInMinutes(job.Interval)
                                            .RepeatForever());
                                    }
                                    break;
                                case IntervalType.HOURS:
                                    {
                                        runtimeJob.Item2.WithSimpleSchedule(x => x
                                            .WithIntervalInHours(job.Interval)
                                            .RepeatForever());
                                    }
                                    break;
                            }

                            await _scheduler.ScheduleJob(runtimeJob.Item1, runtimeJob.Item2.Build());
                        }
                    }
                }

                Logger.Info($"Version: {app.Version}");

                await Task.Delay(TimeSpan.FromSeconds(30));

                await Task.Delay(-1, cancellationTokenSource.Token).ContinueWith(t => { });
            }
            catch (SchedulerException ex)
            {
                Logger.Fatal($"{ex.Message}");
            }

            LogManager.Shutdown();

            return 0;
        }

        private static void UnhandledExceptionHandler(object sender, UnhandledExceptionEventArgs args)
        {
            Exception e = (Exception)args.ExceptionObject;
            Logger.Fatal($"{e.Message}");
            Logger.Fatal($"{args.IsTerminating}");
        }

        private static List<PluginLoader> GetPluginLoaders()
        {
            var loaders = new List<PluginLoader>();

            var pluginsDir = Path.Combine(AppContext.BaseDirectory, "strategies");
            foreach (var dir in Directory.GetDirectories(pluginsDir))
            {
                var dirName = Path.GetFileName(dir);
                var pluginDll = Path.Combine(dir, $"Solbo.Strategy.{dirName}.dll");
                if (File.Exists(pluginDll))
                {
                    var loader = PluginLoader.CreateFromAssemblyFile(
                        pluginDll,
                        sharedTypes: new[] { typeof(IStrategy), typeof(IServiceCollection) });
                    loaders.Add(loader);
                }
            }

            return loaders;
        }
    }
}