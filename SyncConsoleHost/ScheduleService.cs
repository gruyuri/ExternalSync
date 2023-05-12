using CommonLib;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Extensions.Hosting;
using Quartz;
using Microsoft.Extensions.DependencyInjection;
using SyncConsoleHost.Utils;

namespace SyncConsoleHost
{
    public class ScheduleService
    {
        private const string MAIN_GROUP = "groupMain";
        private const string LOW_GROUP = "groupLow";


        private IHost host = null;
        private ConfigReader svcConfigReader = new ConfigReader();

        private AppDomain dllDomain = null;

        public AppDomain DllDomain
        {
            get
            {
                #pragma warning disable SYSLIB0024

                if (dllDomain == null)
                    dllDomain = AppDomain.CreateDomain(Consts.APP_DOMAIN_NAME);

                #pragma warning restore SYSLIB0024

                return dllDomain;
            }
        }

        public IHost ScheduleHost
        {
            get
            {
                if (host == null)
                    host = CreateScheduleHost();

                return host;
            }
        }

        private IHost CreateScheduleHost()
        {
            var builder = Host.CreateDefaultBuilder()
                .ConfigureServices((cxt, services) =>
                {
                    services.AddQuartz(q =>
                    {
                        q.UseMicrosoftDependencyInjectionJobFactory();
                    });
                    services.AddQuartzHostedService(opt =>
                    {
                        opt.WaitForJobsToComplete = true;
                    });
                }).Build();

            return builder;
        }

        public async void StartModules()
        {
            var modules = svcConfigReader.ReadModulesFromConfig();

            await StartProcessing(modules);
        }

        public void StopModules() { }

        public void RefreshSchedules()
        {

        }

        private async Task StartProcessing(List<ModuleDescriptor> modules)
        {
            foreach (var module in modules)
            {
                var moduleInstance = CreateInstance(module.AssemblyName);

                if (moduleInstance != null)
                {
                    await AddJobToSchedule(module, moduleInstance);
                    Console.WriteLine($"Registered schedule for Job {module.ModuleName}");
                }
                //else
                //{
                //    Console.WriteLine($"Invalid assembly {module.AssemblyName}");
                //}
            }

            // Local process to re-read config: compare with Name + fullName + cronExpr

            await ScheduleHost.RunAsync();
        }

        private async Task AddJobToSchedule(ModuleDescriptor module, IExecutable moduleInstance)
        {
            JobDataMap jobDataMap = new JobDataMap();
            jobDataMap.Add(Consts.MODULE_INSTANCE_KEY, moduleInstance);

            IJobDetail job = JobBuilder.Create<ModuleProcessor>()
                .WithIdentity(name: module.ModuleName, group: MAIN_GROUP)
                .SetJobData(jobDataMap)
                .Build();

            ITrigger trigger = TriggerBuilder.Create()
                .WithIdentity(name: module.ModuleName, group: MAIN_GROUP)
                .WithCronSchedule(module.CronExpr)
                    .ForJob(job)
                    .Build();

            var schedulerFactory = ScheduleHost.Services.GetRequiredService<ISchedulerFactory>();
            var scheduler = await schedulerFactory.GetScheduler();

            // Tell Quartz to schedule the job using our trigger
            await scheduler.ScheduleJob(job, trigger);
        }

        private IExecutable CreateInstance(string assemblyFullName)
        {
            try
            {
                //var decoupledAssembly = DllDomain.Load(assemblyFullName);

                // Load the assembly. This works.
                var decoupledAssembly = Assembly.LoadFrom(assemblyFullName);
                if (decoupledAssembly == null)
                {
                    throw new ApplicationException($"Unable to load assembly {assemblyFullName}");
                }

                Type[] types = decoupledAssembly.GetTypes();
                foreach (Type type in types)
                {
                    // Does this class support the transport interface?
                    Type typeExample = type.GetInterface("IExecutable");
                    if (typeExample == null)
                    {
                        // Not supported.
                        continue;
                    }

                    // This class supports the interface. Instantiate it.
                    IExecutable example = decoupledAssembly.CreateInstance(type.FullName) as IExecutable;
                    if (example != null)
                    {
                        return example;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            // Assembly is invalid - no entry for IExecutable
            return null;
        }
    }
}
