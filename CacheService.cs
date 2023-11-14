using System;
using System.Configuration;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace CacheServerConcole
{
    public class CacheService
    {
        private static Dictionary<string, object> cache = new Dictionary<string, object>();
        public static event EventHandler<CacheEvent> CacheUpdated;
        private static int port;

        public void Start()
        {
            System.Diagnostics.Debugger.Launch();
            ReadConfig();
            StartServer();
        }

        public void Stop()
        {
            StopServer();
        }

        public static void NotifyCacheUpdated(CacheEventType eventType, string key, object value, string client)
        {
            CacheUpdated?.Invoke(null, new CacheEvent(eventType, key, value));
            Console.WriteLine("NotifyCacheUpdated :: eventType: " + eventType + " | " + "key: " + key + " | " + "value: " + value + " | " + "client: " + client);
        }

        private static void StartServer()
        {
            TcpListener listener = new TcpListener(IPAddress.Any, port);
            listener.Start();
            Console.WriteLine("Cache Server started on port " + port);

            while (true)
            {
                TcpClient client = listener.AcceptTcpClient();
                Thread clientThread = new Thread(HandleClient);
                clientThread.Start(client);
            }
        }

        private static void StopServer()
        {
/**/
        }

        private static void HandleClient(object clientObj)
        {
            TcpClient client = (TcpClient)clientObj;
            NetworkStream stream = client.GetStream();

            /*using (MemoryStream ms = new MemoryStream())
              {
                  int numBytesRead;
                  while ((numBytesRead = stream.Read(data, 0, data.Length)) > 0)
                  {
                      ms.Write(data, 0, numBytesRead);
                  }
                  str = Encoding.ASCII.GetString(ms.ToArray(), 0, (int)ms.Length);
              }*/

            byte[] buffer = new byte[1024];
            int bytesRead = stream.Read(buffer, 0, buffer.Length);
            //  Console.Write(Encoding.Default.GetString(buffer));
            string request = Encoding.ASCII.GetString(buffer, 0, bytesRead);

            string[] requestParts = request.Split('|');
            string operation = requestParts[0];
            string key = requestParts[1];

            string response = "";

            if (operation == "GET")
            {
                if (cache.ContainsKey(key))
                {
                    response = cache[key].ToString();
                }
            }
            else if (operation == "ADD")
            {
                if (requestParts.Length > 2)
                {
                    string value = requestParts[2];
                    string client_name = requestParts[3];
                    cache[key] = value;
                    response = "Added";
                    NotifyCacheUpdated(CacheEventType.Add, key, value, client_name);

                }
            }
            else if (operation == "REMOVE")
            {
                if (cache.ContainsKey(key))
                {
                    cache.Remove(key);
                    response = "Removed";
                }
            }
            else if (operation == "CLEAR")
            {
                cache.Clear();
                response = "Cache Cleared";

            }
            else if (operation == "DISPOSE")
            {
                cache.Clear();
                response = "Cache Disposed";
            }
            else if (operation == "INIT")
            {
                if (cache.ContainsKey(key))
                {
                    response = cache[key].ToString();
                }
            }

            byte[] responseBytes = Encoding.ASCII.GetBytes(response);

            stream.Write(responseBytes, 0, responseBytes.Length);

            client.Close();
        }

        private static void ReadConfig()
        {
            try
            {
                var serverPort = ConfigurationManager.AppSettings["ServerPort"];
                if (!string.IsNullOrEmpty(serverPort))
                {
                    port = int.Parse(serverPort);
                }
                else
                {
                    Console.WriteLine("ServerPort not found in app.config. Using default port.");
                }
            }
            catch (ConfigurationErrorsException)
            {
                Console.WriteLine("Error reading app.config. Using default port.");
            }
        }
    }
}
