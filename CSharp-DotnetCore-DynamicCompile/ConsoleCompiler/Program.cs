/*************************************************************************************
此代码运行后将ScrCode2Compile/Order.txt作为代码进行编译到 bin\Debug\netcoreapp2.0\Compilee.dll
*************************************************************************************/

using System;
using System.Collections.Generic;

namespace ConsoleCompiler
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Compiling");
            var compiler = new RoslynCompilerService();
            compiler.Compile(new List<string> { "ScrCode2Compile\\Order.txt" }, "Compilee", "Compilee.dll");
        }
    }
}
