using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Debug_Server
{
    class Program
    {
        static void Main(string[] args)
        {
        
            Console.Title = "Server";
            AsyncServer server = new AsyncServer();
            server.SetupServer();
            Console.ReadKey();
            Console.WriteLine("Server has been closed");
        }
     
    }
}
