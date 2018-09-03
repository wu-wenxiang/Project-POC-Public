using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Configuration;

namespace TestLoadDLL
{
    class Program
    {
        static void Main(string[] args)
        {
            string appSettingValue = ConfigurationManager.AppSettings["dllPath"];
            Console.WriteLine(appSettingValue);
            var aDLL = Assembly.LoadFile(appSettingValue);
            foreach (Type type in aDLL.GetExportedTypes())
            {
                Console.WriteLine(type);
                //var c = Activator.CreateInstance(type);
                //type.InvokeMember("Output", BindingFlags.InvokeMethod, null, c, new object[] { @"Hello" });
            }
            Console.ReadLine();
        }
    }
}
