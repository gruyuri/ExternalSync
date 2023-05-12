using CommonLib;
using Quartz;
using SyncConsoleHost.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncConsoleHost
{
    public class ModuleProcessor : Quartz.IJob
    {
        public Task Execute(IJobExecutionContext context)
        {
            JobKey key = context.JobDetail.Key;

            JobDataMap dataMap = context.MergedJobDataMap;

            var moduleInstance = (IExecutable)dataMap[Consts.MODULE_INSTANCE_KEY];

            return moduleInstance.Start();
        }
    }
}
