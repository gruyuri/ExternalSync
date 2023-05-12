using CommonLib;
using Newtonsoft.Json;
using SyncConsoleHost;
using System.Dynamic;
using System.Reflection;

Console.WriteLine("Starting ExternalSync ...");

var svc = new ScheduleService();

svc.StartModules();

Console.ReadLine();

// On StopService() ->
svc.StopModules();
