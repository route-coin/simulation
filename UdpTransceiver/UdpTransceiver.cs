using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Common
{
    public class UdpTransceiver
    {
        const int PORT_NUMBER = 15000;

        private string _callerPublicKey;
        private string _callerPassword;

        Thread t = null;
        public void Start(string callerPublicKey, string callerPassword)
        {
            if (t != null)
            {
                throw new Exception("Already started, stop first");
            }

            _callerPublicKey = callerPublicKey;
            _callerPassword = callerPassword;

            Console.WriteLine("Started listening");
            StartListening();
        }
        public void Stop()
        {
            try
            {
                udp.Close();
                Console.WriteLine("Stopped listening");
            }
            catch { /* don't care */ }
        }

        private readonly UdpClient udp = new UdpClient(PORT_NUMBER);
        IAsyncResult ar_ = null;

        private void StartListening()
        {
            ar_ = udp.BeginReceive(Receive, new object());
        }

        private void Receive(IAsyncResult ar)
        {
            IPEndPoint ip = new IPEndPoint(IPAddress.Any, PORT_NUMBER);
            byte[] bytes = udp.EndReceive(ar, ref ip);
            string message = Encoding.ASCII.GetString(bytes);
            Console.WriteLine($"From {ip.Address.ToString()} received: {message}");

            Task.Run(async () =>
            {
                var result = await ProcessMessage(message);
            }).GetAwaiter().GetResult();

            StartListening();
        }

        private async Task<string> ProcessMessage(string message)
        {
            try
            {
                if (!message.Contains("@"))
                {
                    Console.WriteLine($"Invalid Message");
                    throw new ArgumentException();
                }

                var messageType = message.Split('@')[0];
                var messageBody = message.Split('@')[1];
                Console.WriteLine($"Processing command:{messageType}");
                if (messageType == "ContractSubmitted")
                {
                    var result = await ProcessRouteFound(messageBody);
                    if (string.IsNullOrEmpty(result))
                        Console.WriteLine($"Processing {messageType} failed.");
                    else
                    {
                        Console.WriteLine($"RouteFound submitted. Contract address: {result}");
                        Send($"RouteFound@{result}");
                    }
                }
                return string.Empty;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error:{ ex.Message}");
                return string.Empty;
            }

            
        }

        private async Task<string> ProcessRouteFound(string messageBody)
        {
            var contractHelper = new ContractHelper();
            Console.WriteLine($"Unlocking account with: {_callerPublicKey}, {_callerPassword}");
            var unlocked = await contractHelper.UnlockAccount(_callerPublicKey, _callerPassword);
            Console.WriteLine($"Account Unlocked: {unlocked}");
            return await contractHelper.RouteFound(messageBody, _callerPublicKey);
        }

        public void Send(string message)
        {
            UdpClient client = new UdpClient();
            IPEndPoint ip = new IPEndPoint(IPAddress.Parse("255.255.255.255"), PORT_NUMBER);
            byte[] bytes = Encoding.ASCII.GetBytes(message);
            client.Send(bytes, bytes.Length, ip);
            client.Close();
            Console.WriteLine("Sent: {0} ", message);
        }
    }
}
