using System.Reflection;

namespace PluginTest
{
    /// <summary>
    /// A class to use an external managed plugin library
    /// </summary>
    internal class ManagedPlugin : IRomcenterPlugin
    {
        private readonly Assembly assembly;

        /// <summary>
        /// Create an instance linked to an external managed plugin library.
        /// </summary>
        /// <param name="pluginLibraryPath"></param>
        public ManagedPlugin(string pluginLibraryPath)
        {
            try
            {
                Assembly asm = Assembly.LoadFrom(pluginLibraryPath);
                assembly = asm;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
                throw;
            }
        }

        public string? GetSignature(string filename, string zipcrc, out string format, out long size, out string comment, out string errorMessage)
        {
            //init
            format = "";
            size = 0;
            comment = "";
            errorMessage = "";

            var parameters = new object?[] { filename, zipcrc, null, null, null, null };

            var (result, param) = GetMethodResult(nameof(GetSignature), parameters);

            if (result == null || param == null)
            {
                return null;
            }

            format = (string)(param[2] ?? "");
            size = (long)(param[3] ?? 0);
            comment = (string)(param[4] ?? "");
            errorMessage = (string)(param[5] ?? "");

            return (string)result;
        }

        public string GetAuthor()
        {
            return GetSimpleResult(nameof(GetAuthor));
        }

        public string GetDescription()
        {
            return GetSimpleResult(nameof(GetDescription));
        }

        public string GetDllInterfaceVersion()
        {
            return GetSimpleResult(nameof(GetDllInterfaceVersion));
        }

        public string GetDllType()
        {
            return GetSimpleResult(nameof(GetDllType));
        }

        public string GetEmail()
        {
            return GetSimpleResult(nameof(GetEmail));
        }

        public string GetPlugInName()
        {
            return GetSimpleResult(nameof(GetPlugInName));
        }

        public string GetVersion()
        {
            return GetSimpleResult(nameof(GetVersion));
        }

        public string GetWebPage()
        {
            return GetSimpleResult(nameof(GetWebPage));
        }

        private string GetSimpleResult(string methodName)
        {
            var (result, _) = GetMethodResult(methodName);

            if (result == null)
            {
                return "";
            }
            return (string)result;
        }

        private (object? invoke, object?[]? parameters) GetMethodResult(string methodName, object?[]? parameters = null)
        {
            parameters ??= [];

            //we want to create RcPlugin instance from the assembly, but we don't know the RcPlugin class in this project
            //and we don't want to reference plugin project just to get the RcPlugin class.
            //Instead, we use the interface and we get the type which implement IRomcenterPlugin
            Type? t = assembly.DefinedTypes.ToList().FirstOrDefault(t =>
                (t.ImplementedInterfaces.Any(i => i.Name == nameof(IRomcenterPlugin))));

            if (t == null)
            {
                return (null, null);
            }

            //Create an instance to invoke the method
            var instance = Activator.CreateInstance(t);
            if (instance == null)
            {
                return (null, null);
            }

            //Get the method
            var methodInfo = t.GetMethod(methodName);

            if (methodInfo == null)
            {
                return (null, null);
            }

            //invoke
            var invoke = methodInfo.Invoke(instance, parameters);
            return (invoke, parameters);
        }
    }
}
