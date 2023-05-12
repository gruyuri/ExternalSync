namespace CommonLib
{
    public interface IExecutable
    {
        Task Start();

        string Info(string hostName);
    }
}