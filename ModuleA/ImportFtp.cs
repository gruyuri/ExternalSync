using CommonLib;

namespace ModuleA
{
    [Serializable]
    public class ImportFtp : IExecutable
    {
        public string Info(string hostName)
        {
            return $"Description for FTP Import: {hostName}";
        }

        public Task Start()
        {
            Console.WriteLine($"ModuleA: Starting process at {DateTime.Now}");

            return Task.CompletedTask;
        }
    }
}