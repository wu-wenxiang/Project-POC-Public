/*************************************************************************************
此工程模拟开发人员引用运行时生成的程序集进行后续开发
运行ConsoleCompiler后引用其生成的程序集即..\ConsoleCompiler\bin\Debug\netcoreapp2.0\Compilee.dll
*************************************************************************************/
using System;

namespace DevelopersProject
{
    class Program
    {
        static void Main(string[] args)
        {
            var order = new Compilee.Order();
            DateTime date = order.CreateDate;
        }
    }
}