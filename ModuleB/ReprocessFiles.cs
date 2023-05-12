using CommonLib;

namespace ModuleB
{
    [Serializable]
    public class ReprocessFiles : IExecutable
    {
        public string Info(string hostName)
        {
            return $"Description for ModuleB: {hostName}";
        }

        public Task Start()
        {
            Console.WriteLine($"ModuleB: Starting process at {DateTime.Now}");
            return Task.CompletedTask;
        }
    }
}