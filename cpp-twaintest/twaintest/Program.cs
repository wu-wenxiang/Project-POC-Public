using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices;


namespace twaintest
{
    class Program
    {
        [DllImport("twaintestlib.dll")]
        internal static extern int loadtwain();
        [DllImport("twaintestlib.dll")]
        internal static extern int call_dsm_entry();

        [DllImport("twaintestlib.dll")]
        internal static extern int call_logmsg();
        static void Main(string[] args)
        {
           
            loadtwain();

            Thread th1 = new Thread(dowork);
            Thread th2 = new Thread(dowork);
            th1.Start();
            th2.Start();
            Console.WriteLine("Press Any Key to stop....");
            Console.ReadLine();
            
        }
        public static void dowork()
        {
            while (1==1)
            {
                //call_dsm_entry();
                call_logmsg();
            }
        }
    }
}
