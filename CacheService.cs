using log4net;
using log4net.Config;
using Log4NetSample.LogUtility;
using System;
using System.Configuration;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;

namespace CacheServerConcole
{
    public class CacheService:ICache
    {
        private static Dictionary<string, TcpClient> clients = new Dictionary<string, TcpClient>();
        public static event EventHandler<CacheEvent> CacheUpdated;
        private static readonly object _cacheLock = new object();
        private static readonly int DEFAULT_CACHE_SIZE = 5;
        private static Dictionary<string, object> cache;
        private static string[] requestParts = default;
        private static Cache<string, string> myCache;
        private static int mNumberOfClients = 0;
        private static Logger serverCacheLogger;
        private static string operation = "";
        private static string response = "";
        private static string key = "";
        private static int cacheSize;
        private static ICache icache;
        private static int port;
        private static ILog log;
        private static TcpListener listener;
        private static TcpClient client;
        private static Thread clientThread;
        /// <summary>
        /// Cache operations enumerations
        /// </summary>
        public enum CacheOperations
        {
            Init,
            Add,
            Update,
            Remove,
            Get,
            Clear,
            Dispose,
            Sub,
            UnSub
        }

        public enum CacheResponseOps
        { 
            Success = 1, 
            Failure = 2,
            CacheNotInitialized = 3,
            CacheExpired = 4,
            NoValueFound = 5,
            NoCacheFound = 6,
            DuplicateValue = 7,
            NoKeyFound = 8,
            CacheAlreadyInitialized = 9
        }
        /// <summary>
        /// To start a service
        /// </summary>
        public void Start()
        {
            System.Diagnostics.Debugger.Launch();
            serverCacheLogger = GetCacherLogger();
            icache = new CacheService();
            ReadConfig();
            StartServer();
        }
        /// <summary>
        /// To Stop service and release resources
        /// </summary>
        public void Stop()
        {
            StopServer();
        }
        /// <summary>
        /// To start a tcp server and client thread
        /// </summary>
        private static void StartServer()
        {
            try
            {
                listener = new TcpListener(IPAddress.Any, port);
                listener.Start();
                serverCacheLogger.Info("Cache Server started on port " + port);

                while (true)
                {
                    StartBackgroundthread();
                    client = listener.AcceptTcpClient();
                   // serverCacheLogger.Info("Connected " + ((IPEndPoint)client.Client.RemoteEndPoint).Address);
                    clientThread = new Thread(HandleClient);
                    clientThread.Start(client);
                }
            } 
            catch (Exception e)
            {
                serverCacheLogger.Error("Server Error {0}: ", e.InnerException);
            }
        }
        /// <summary>
        /// To stop a server and release resources
        /// </summary>
        private static void StopServer()
        {
            listener.Stop();
            serverCacheLogger.Info("Cache Server stopped");
            client.Close();
            serverCacheLogger.Info("Client closed!");
        }
        /// <summary>
        /// To handle client requests
        /// </summary>
        /// <param name="clientObj"></param>
        private static void HandleClient(object clientObj)
        {
            try
            {
                TcpClient client = (TcpClient)clientObj;
                NetworkStream stream = client.GetStream();

                byte[] buffer = new byte[1024];
                int bytesRead = stream.Read(buffer, 0, buffer.Length);
                string request = Encoding.ASCII.GetString(buffer, 0, bytesRead);

                requestParts = request.Split('|');
                operation = requestParts[0];
                key = requestParts[1];

                if (operation == CacheOperations.Get.ToString())
                {
                    response = (string)icache.Get(key);
                }
                else if (operation == CacheOperations.Add.ToString())
                {
                    icache.Add(key, requestParts[2], int.Parse(requestParts[3]));
                    /*foreach (KeyValuePair<string, TcpClient> remoteClient in clients)
                    {
                        if (remoteClient.Value != client)
                        {
                            response = CacheOperations.Add.ToString();
                            NetworkStream stm = remoteClient.Value.GetStream();
                            byte[] responseByte = Encoding.ASCII.GetBytes(response);
                            stm.Write(responseByte, 0, responseByte.Length);
                        }
                    }*/
                }
                else if (operation == CacheOperations.Update.ToString())
                {
                    icache.Update(key, requestParts[2], int.Parse(requestParts[3]));
                    /* foreach (KeyValuePair<string, TcpClient> remoteClient in clients)
                     {
                         if (remoteClient.Value != client)
                         {
                             response = CacheOperations.Update.ToString();
                             NetworkStream stm = remoteClient.Value.GetStream();
                             byte[] responseByte = Encoding.ASCII.GetBytes(response);
                             stm.Write(responseByte, 0, responseByte.Length);                        
                         }
                     }*/
                }
                else if (operation == CacheOperations.Remove.ToString())
                {
                    icache.Remove(key);
                    foreach (KeyValuePair<string, TcpClient> remoteClient in clients)
                    {
                        if (remoteClient.Value != client)
                        {
                            if (remoteClient.Value.Connected)
                            {
                                response = CacheOperations.Remove.ToString();
                                NetworkStream newStream = remoteClient.Value.GetStream();
                                byte[] responseByte = Encoding.ASCII.GetBytes(response);
                                newStream.Write(responseByte, 0, responseByte.Length);
                            }
                        }
                    }
                }
                else if (operation == CacheOperations.Clear.ToString())
                {
                    icache.Clear();
                }
                else if (operation == CacheOperations.Dispose.ToString())
                {
                    icache.Dispose();
                }
                else if (operation == CacheOperations.Init.ToString())
                {
                    icache.Initialize();
                }
                else if (operation == CacheOperations.Sub.ToString())
                {
                    mNumberOfClients++;
                    if (!clients.ContainsKey("user" + mNumberOfClients))
                    {
                        clients.Add("user" + mNumberOfClients, client);
                    }
                    serverCacheLogger.Info(clients.Count + "---> " + "user" + mNumberOfClients);
                    icache.SubscribeToCacheUpdates();
                }
                else if (operation == CacheOperations.UnSub.ToString())
                {
                    mNumberOfClients--;
                    if (clients.ContainsKey("user" + mNumberOfClients))
                    {
                        clients.Remove("user" + mNumberOfClients);
                    }
                    serverCacheLogger.Info(clients.Count + "---> " + "user" + mNumberOfClients);
                    icache.UnsubscribeFromCacheUpdates();
                }

                byte[] responseBytes = Encoding.ASCII.GetBytes(response);
                stream.Write(responseBytes, 0, responseBytes.Length);
            }
            catch (Exception ex)
            {
                serverCacheLogger.Error("Server Exception: {0}", ex.InnerException);
                response = ex.Message;
            }
        }
        /// <summary>
        /// To initialize cache on server
        /// </summary>
        public void Initialize()
        {
            if (myCache == null)
            {
                myCache = new Cache<string, string>(getCacheSize(), serverCacheLogger);
                response = myCache.InitializeCache();
                NotifyCacheUpdated(CacheEventType.Init, key);
            }
            else
            {
                response = "Cache already Initialized!";
            }
        }
        /// <summary>
        /// Add operation to server cache
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="expirationSeconds"></param>
        public void Add(string key, object value, int? expirationSeconds = null)
        {
            if (myCache != null)
            {
                if (expirationSeconds == null)
                {
                    expirationSeconds = Int32.MaxValue;
                }
                response = myCache.Add(key, (string)value, new TimeSpan(0, 0, seconds: (int)expirationSeconds));
                //response = ResponseProtocol(myCache.Add(key, (string)value, new TimeSpan(0, 0, seconds: (int)expirationSeconds)), key, (string)value);
                
                NotifyCacheUpdated(CacheEventType.Add, key, value);
            }
            else
            {
                response = "Please initialize Cache!";
                NotifyCacheUpdated(CacheEventType.Add, exMsg: response);
            }
        }
        /// <summary>
        /// Update operation of server cache
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="expirationSeconds"></param>
        public void Update(string key, object value, int? expirationSeconds = null)
        {
            if (myCache != null)
            {
                if (expirationSeconds == null)
                {
                    expirationSeconds = Int32.MaxValue;
                }
                response = myCache.Update(key, (string)value, new TimeSpan(0, 0, seconds: (int)expirationSeconds));
                NotifyCacheUpdated(CacheEventType.Update, key, value);
            }
            else
            {
                response = "Please initialize Cache!";
                NotifyCacheUpdated(CacheEventType.Get, exMsg: response);
            }
        }
        /// <summary>
        /// Remove operation of server cache
        /// </summary>
        /// <param name="key"></param>
        public void Remove(string key)
        {
            if (myCache != null)
            {
                response = myCache.Remove(key);
                NotifyCacheUpdated(CacheEventType.Remove, key);
            }
            else
            {
                response = "Cache instance is null";
                NotifyCacheUpdated(CacheEventType.Get, exMsg: response);
            }
        }
        /// <summary>
        /// Get server cache based of key
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public object Get(string key)
        {
            if (myCache == null)
            {
                response = "No Cache Found!";
                NotifyCacheUpdated(CacheEventType.Get, exMsg: response);
                return response;
            }
            else
            {
                response = myCache.Get(key);
                NotifyCacheUpdated(CacheEventType.Get, key);
                return response;
            }
        }
        /// <summary>
        /// Clear server cache
        /// </summary>
        public void Clear()
        {
            response = myCache.Clear();
            NotifyCacheUpdated(CacheEventType.Clear, key);
        }
        /// <summary>
        /// Dispose server cache
        /// </summary>
        public void Dispose()
        {
            response = myCache.Dispose();
            myCache = null;
            NotifyCacheUpdated(CacheEventType.Dispose, key);
        }
        /// <summary>
        /// Subscription for cache events on server
        /// </summary>
        public void SubscribeToCacheUpdates()
        {
            response = CacheOperations.Sub.ToString() + "Subscribed!";
        }
        /// <summary>
        /// To unsubscribe from server cache events
        /// </summary>
        public void UnsubscribeFromCacheUpdates()
        {
            response = CacheOperations.UnSub.ToString() + "Unsubscribed!";
        }
        /// <summary>
        /// To log server events (All cache operations)
        /// </summary>
        /// <param name="eventType"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="exMsg"></param>
        public static void NotifyCacheUpdated(CacheEventType eventType, string? key = null, object? value = null, string? exMsg = null)
        {
            CacheUpdated?.Invoke(null, new CacheEvent(eventType, key, value));
            serverCacheLogger.Info("NotifyCacheUpdated :: eventType: " + eventType + " | " + "key: " + key + " | " + "value: " + value + " | " + "ExMessage: " + exMsg);
        }
        /// <summary>
        /// To get cache size from config
        /// </summary>
        /// <returns></returns>
        private static int getCacheSize()
        {
            try
            {
                var size = ConfigurationManager.AppSettings["cacheSize"];
                if (!string.IsNullOrEmpty(size))
                {
                    return cacheSize = int.Parse(size);
                }
                else
                {
                    serverCacheLogger.Info("cacheSize not found in app.config. Using default size.");
                    return DEFAULT_CACHE_SIZE;
                }
            }
            catch (ConfigurationErrorsException e)
            {
                serverCacheLogger.Error("Error reading app.config. Using default size.", e.InnerException);
                return DEFAULT_CACHE_SIZE;
            }
        }
        /// <summary>
        /// To read port from config file
        /// </summary>
        private static void ReadConfig()
        {
            try
            {
                var serverPort = ConfigurationManager.AppSettings["port"];
                if (!string.IsNullOrEmpty(serverPort))
                {
                    port = int.Parse(serverPort);
                }
                else
                {
                    serverCacheLogger.Info("ServerPort not found in app.config. Using default port.");
                }
            }
            catch (ConfigurationErrorsException e)
            {
                serverCacheLogger.Error("Error reading app.config. Using default port.", e.InnerException);
            }
        }
        /// <summary>
        /// Background thread to execute eviction policy
        /// </summary>
        public static void StartBackgroundthread()
        {
            try
            {
                Thread backgroundThread = new Thread(EvictionPolicyTimer);
                backgroundThread.IsBackground = true;
                backgroundThread.Start();
            }
            catch (ArgumentNullException e)
            {
                serverCacheLogger.Error("Background thread exception: {0}", e.InnerException);
            }
            catch (ThreadStateException e)
            {
                serverCacheLogger.Error("Background thread exception: {0}", e.InnerException);
            }
            catch (Exception e)
            {
                serverCacheLogger.Error("Background thread exception: {0}", e.InnerException);
            }
        }
        /// <summary>
        /// Eviction Timer
        /// </summary>
        public static void EvictionPolicyTimer()
        {
            try
            {
                Timer timer = new Timer(new TimerCallback(ApplyEvictionPolicy), null, 20000, System.Threading.Timeout.Infinite);
            }
            catch (ArgumentNullException e)
            {
                serverCacheLogger.Error("Timer exception: {0}", e.InnerException);
            }
            catch (ArgumentOutOfRangeException e)
            {
                serverCacheLogger.Error("Timer exception: {0}", e.InnerException);
            }
            catch (Exception e)
            {
                serverCacheLogger.Error("Timer exception: {0}", e.InnerException);
            }
        }
        /// <summary>
        /// Apply eviction policy to cache
        /// </summary>
        /// <param name="obj"></param>
        private static void ApplyEvictionPolicy(object? obj)
        {
            serverCacheLogger.Info("EvictionPolicy **Called**");
            if (myCache != null)
            {
                myCache.ExecuteEvictionPolicy();
            }
        }
        /// <summary>
        /// To get server cache logger instance[log4net]
        /// </summary>
        /// <returns></returns>
        public static Logger GetCacherLogger()
        {
            try
            {
                var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
                XmlConfigurator.Configure(logRepository, new FileInfo("log4netconfig.config"));
                return new Logger();
            }
            catch (Exception e)
            {
                Console.WriteLine("Logger Exception: {0}", e.InnerException);
                return default;
            }
        }
        public static string ResponseProtocol(string response, string? key = null, string? value = null)
        {
            return response + "|" + key + "|" + value;
        }
    }
}
