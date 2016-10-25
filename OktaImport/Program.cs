using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace OktaImport
{
    static class Program
    {
        static void Main(string[] args)
        {

            //allows interactive execution
            if (Environment.UserInteractive)
            {
                ImportUser service1 = new ImportUser();
                service1.TestStartupAndStop(args);
            }
            else
            {

                ServiceBase[] ServicesToRun;
                ServicesToRun = new ServiceBase[] 
                { 
                    new ImportUser() 
                };
                ServiceBase.Run(ServicesToRun);
            }

        }
    }

}
