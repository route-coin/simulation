using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RouteBuyer
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Some arguments missing.");
                Console.WriteLine("Expected arguments:");
                Console.WriteLine("clientPublicKey clientPassword");
                return;
            }

            var clientPublicKey = args[0];
            var clientPassword = args[1];

            var udpTransceiver = new UdpTransceiver();
            udpTransceiver.Start(clientPublicKey, clientPassword);

            ConsoleKeyInfo cki;
            do
            {
                if (Console.KeyAvailable)
                {
                    cki = Console.ReadKey(true);
                    switch (cki.KeyChar)
                    {
                        case 's':

                        case 'x':
                            udpTransceiver.Stop();
                            return;
                    }
                }
                Thread.Sleep(10);
            } while (true);

        }
    }
}
