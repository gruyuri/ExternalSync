using Newtonsoft.Json;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SyncConsoleHost
{
    public class ConfigReader : Quartz.IJob
    {
        public List<ModuleDescriptor> Modules { get; private set; }

        public Task Execute(IJobExecutionContext context)
        {
            Modules = ReadModulesFromConfig();
            return Task.CompletedTask;
        }

        public List<ModuleDescriptor> ReadModulesFromConfig()
        {
            var result = new List<ModuleDescriptor>();

            string fileName = "ModuleSettings.json";

            string fullFileName = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), fileName);

            if (File.Exists(fullFileName))
            {
                try
                {
                    using (StreamReader r = new StreamReader(fullFileName))
                    {
                        string json = r.ReadToEnd();
                        result = JsonConvert.DeserializeObject<List<ModuleDescriptor>>(json);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception in reading config {fileName}: {ex.Message}");
                    result = new List<ModuleDescriptor>();
                }
            }

            return result;
        }
    }
}
