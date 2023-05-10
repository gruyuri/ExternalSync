using CommonLib;
using Newtonsoft.Json;
using SyncConsoleHost;
using System.Dynamic;
using System.Reflection;

Console.WriteLine("Starting ExternalSync ...");

List<ModuleDescriptor> modules = ReadModulesFromConfig();

StartProcessing(modules);

Console.ReadLine();

List<ModuleDescriptor> ReadModulesFromConfig()
{
    var result = new List<ModuleDescriptor>();

    string fileName = "ModuleSettings.json";

    string fullFileName = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) , fileName);

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

void StartProcessing(List<ModuleDescriptor> modules)
{
    foreach (var module in modules)
    {
        var moduleInstance = CreateInstance(module.AssemblyName);

        if (moduleInstance != null)
        {
            moduleInstance.Start();
            Console.WriteLine(moduleInstance.Info("localhost"));
        } else
        {
            Console.WriteLine($"Invalid assembly {module.AssemblyName}");
        }
        Console.WriteLine($"Starting {module.ModuleName} on schedule {module.CronExpr} from assembly {module.AssemblyName}");
    }
}

IExecutable CreateInstance(string assemblyFullName)
{
    try
    {
        // Load the assembly. This works.
        System.Reflection.Assembly decoupledAssembly =
            System.Reflection.Assembly.LoadFrom(assemblyFullName);
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