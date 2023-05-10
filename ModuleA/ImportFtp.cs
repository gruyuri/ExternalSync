using CommonLib;

namespace ModuleA
{
    public class ImportFtp : IExecutable
    {
        public string Info(string hostName)
        {
            return $"Description for FTP Import: {hostName}";
        }

        public void Start()
        {
            Console.WriteLine($"ModuleA: Starting process at {DateTime.Now}");
        }
    }
}