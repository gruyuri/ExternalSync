using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncConsoleHost
{
    public class ModuleDescriptor
    {
        public string ModuleName { get; set; }

        public string CronExpr { get; set; }

        public string AssemblyName { get; set; }
    }
}
