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
using FluentScheduler;
using Quartz.Impl.Matchers;

namespace SyncConsoleHost
{
    public class ScheduleService
    {
        private const string MAIN_GROUP = "groupMain";
        private const string LOW_GROUP = "groupLow";


        private IHost host = null;
        private ConfigReader svcConfigReader = new ConfigReader();

        //private AppDomain dllDomain = null;

        //public AppDomain DllDomain
        //{
        //    get
        //    {
        //        if (dllDomain == null)
        //            dllDomain = AppDomain.CreateDomain(Consts.APP_DOMAIN_NAME);

        //        return dllDomain;
        //    }
        //}

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

        public void StopModules() 
        {
            FluentScheduler.JobManager.Stop();

            ScheduleHost.StopAsync();
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

            JobManager.AddJob(() => RefreshModuleList(),
                        (s) => s.WithName("RefreshModuleList").ToRunEvery(2).Minutes());

            FluentScheduler.JobManager.Start();

            await ScheduleHost.RunAsync();
        }

        private List<JobKey> GetAllJobKeys(IScheduler scheduler)
        {
            var result = new List<JobKey>();

            var jobGroups = scheduler.GetJobGroupNames().Result;

            foreach (string group in jobGroups)
            {
                var groupMatcher = GroupMatcher<JobKey>.GroupContains(group);
                var jobKeys = scheduler.GetJobKeys(groupMatcher).Result;
                result.AddRange(jobKeys);

                //foreach (var jobKey in jobKeys)
                //{
                //    var detail = scheduler.GetJobDetail(jobKey);
                //    var triggers = scheduler.GetTriggersOfJob(jobKey);
                //    foreach (ITrigger trigger in triggers)
                //    {
                //        Console.WriteLine(group);
                //        Console.WriteLine(jobKey.Name);
                //        Console.WriteLine(detail.Description);
                //        Console.WriteLine(trigger.Key.Name);
                //        Console.WriteLine(trigger.Key.Group);
                //        Console.WriteLine(trigger.GetType().Name);
                //        Console.WriteLine(scheduler.GetTriggerState(trigger.Key));
                //        DateTimeOffset? nextFireTime = trigger.GetNextFireTimeUtc();
                //        if (nextFireTime.HasValue)
                //        {
                //            Console.WriteLine(nextFireTime.Value.LocalDateTime.ToString());
                //        }

                //        DateTimeOffset? previousFireTime = trigger.GetPreviousFireTimeUtc();
                //        if (previousFireTime.HasValue)
                //        {
                //            Console.WriteLine(previousFireTime.Value.LocalDateTime.ToString());
                //        }
                //    }
                //}
            }

            return result;
        }

        /// <summary>
        /// Process to re-read module config
        /// </summary>
        private void RefreshModuleList()
        {
            Console.WriteLine("Read module config ...");

            var listDeletedJobKeys = new List<JobKey>();
            

            var schedulerFactory = ScheduleHost.Services.GetRequiredService<ISchedulerFactory>();
            var scheduler = schedulerFactory.GetScheduler().Result;

            var modules = svcConfigReader.ReadModulesFromConfig();

            var newModules = new List<ModuleDescriptor>(modules);

            var jobKeys = GetAllJobKeys(scheduler);
            foreach (var jobKey in jobKeys)
            {
                var validJob = modules.FirstOrDefault(x => String.Compare(x.ModuleName, jobKey.Name, true) == 0);
                if (validJob == null)
                    listDeletedJobKeys.Add(jobKey);
                else
                {
                    var existingModule = newModules.FirstOrDefault(x => String.Compare(x.ModuleName, jobKey.Name, true) == 0);
                    if (existingModule != null)
                        newModules.Remove(existingModule);
                }
            }

            foreach (var jobKey in listDeletedJobKeys)
            {
                scheduler.ResumeJob(jobKey);
                scheduler.DeleteJob(jobKey);
            }

            if (newModules.Any())
                StartProcessing(newModules);

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
