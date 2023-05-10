using CommonLib;

namespace ModuleB
{
    public class ReprocessFiles : IExecutable
    {
        public string Info(string hostName)
        {
            return $"Description for ModuleB: {hostName}";
        }

        public void Start()
        {
            Console.WriteLine($"ModuleB: Starting process at {DateTime.Now}");
        }
    }
}