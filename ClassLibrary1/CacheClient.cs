using ClassLibrary1;
using log4net;
using log4net.Config;
using Log4NetSample.LogUtility;
using Newtonsoft.Json;
using System;
using System.Configuration;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;


/// <summary>
/// Cache client lib class to handle client cache  operations
/// </summary>
public class CacheClient : ICache, ICacheEvents
{
    public event EventHandler<CacheEvent> CacheUpdated;
    public static ClientLogger clientCacheLogger;
    NetworkStream stream = null;
    TcpClient tcpClient = null;
    private static int port;
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
    /// <summary>
    /// Cache client constructor
    /// </summary>
    public CacheClient()
    {
        clientCacheLogger = GetCacherLogger();
        ReadConfigInClass();
    }
    /// <summary>
    /// To read port from code
    /// </summary>
    private static void ReadConfigInClass()
    {
        port = 8081;
    }
    /// <summary>
    /// To handle client add opertaion
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <param name="expirationSeconds"></param>
    public void Add(string key, object value, int? expirationSeconds)
    {
        object res = StreamReadWrite(CacheOperations.Add.ToString() + "|" + key + "|" + value + "|" + expirationSeconds);
        OnItemAdded(key);
    }
    /// <summary>
    /// To handle client cache remove operation
    /// </summary>
    /// <param name="key"></param>
    public void Remove(string key)
    {
        object res = StreamReadWrite(CacheOperations.Remove.ToString() + "|" + key);
        OnItemRemoved(key);
    }
    /// <summary>
    /// To get cache against key
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public object Get(string key)
    {
        return StreamReadWrite(CacheOperations.Get.ToString() + "|" + key);
    }
    /// <summary>
    /// To clear the cache
    /// </summary>
    public void Clear()
    {
        StreamReadWrite(CacheOperations.Clear.ToString() + "|");
    }
    /// <summary>
    /// To Dispose cache
    /// </summary>
    public void Dispose()
    {
        StreamReadWrite(CacheOperations.Dispose.ToString() + "|");
    }
    /// <summary>
    /// To Initialize cache instance
    /// </summary>
    void ICache.Initialize()
    {
        StreamReadWrite(CacheOperations.Init.ToString() + "|");
    }
    /// <summary>
    /// To Update cache against key
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <param name="expirationSeconds"></param>
    void ICache.Update(string key, object value, int? expirationSeconds)
    {
        object res = StreamReadWrite(CacheOperations.Update.ToString() + "|" + key + "|" + value + "|" + expirationSeconds);
        OnItemUpdated(key);
    }
    /// <summary>
    /// Handler method to handle cache events
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void OnCacheUpdated(object sender, CacheEvent e)
    {
        clientCacheLogger.Info("NotifyCacheUpdated :: eventType: " + e.EventType + " | " + "Key: " + e.Key);
    }
    /// <summary>
    /// To subscribe for cache events
    /// </summary>
    /// <param name="cacheEvents"></param>
    void ICache.SubscribeToCacheUpdates(ICacheEvents cacheEvents)
    {
        StreamReadWrite(CacheOperations.Sub.ToString() + "|");
        CacheUpdated += OnCacheUpdated;
    }
    /// <summary>
    /// to unsubscribe from cache events
    /// </summary>
    /// <param name="cacheEvents"></param>
    void ICache.UnsubscribeFromCacheUpdates(ICacheEvents cacheEvents)
    {
        StreamReadWrite(CacheOperations.UnSub.ToString() + "|");
        CacheUpdated -= OnCacheUpdated;
    }
    /// <summary>
    /// To read and write from stream
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    public object StreamReadWrite(string request)
    {
        try
        {
            EnsureTcpConnection();
            stream = tcpClient.GetStream();

            byte[] requestBytes = Encoding.ASCII.GetBytes(request);
            stream.Write(requestBytes, 0, requestBytes.Length);
            string response = "";
            byte[] responseBytes = new byte[1024];
            int bytesRead = stream.Read(responseBytes, 0, responseBytes.Length);
            response = Encoding.ASCII.GetString(responseBytes, 0, bytesRead);

            clientCacheLogger.Info(response);
            stream.Close();
            return response;



        }
        catch (ArgumentNullException e)
        {
            clientCacheLogger.Error("ArgumentNullException: {0}", e);
            return e.Message;
        }
        catch (SocketException e)
        {
            clientCacheLogger.Error("SocketException: {0}", e);
            return e.Message;
        }
        catch (Exception ex)
        {
            clientCacheLogger.Error("Client Exception: {0}", ex.InnerException);
            return ex.Message;
        }

    }
    /// <summary>
    /// To read stream from tcp client
    /// </summary>
    public void StreamRead()
    {
        try
        {
            EnsureTcpConnection();
            stream = tcpClient.GetStream();
            byte[] responseBytes = new byte[1024];

            int bytesRead = stream.Read(responseBytes, 0, responseBytes.Length);
            string response = Encoding.ASCII.GetString(responseBytes, 0, bytesRead);
            clientCacheLogger.Info(response);
        }
        catch (ArgumentNullException e)
        {
            clientCacheLogger.Error("ArgumentNullException: {0}", e);
        }
        catch (SocketException e)
        {
            clientCacheLogger.Error("SocketException: {0}", e);
        }
        catch (Exception ex)
        {
            clientCacheLogger.Error("Exception: {0}", ex);
        }
    }
    /// <summary>
    /// To get tcp client an connect to respective IP and port
    /// </summary>
    private void EnsureTcpConnection()
    {
        try
        {
            if (tcpClient == null || !tcpClient.Connected)
            {
                tcpClient = new TcpClient();
                tcpClient.Connect("localhost", port);
                tcpClient.NoDelay = true;
            }
        }
        catch (Exception ex)
        {
            clientCacheLogger.Error("TcpClient Exception: {0}", ex.InnerException);
        }
    }
    /// <summary>
    /// to read port from config 
    /// </summary>
    private static void ReadConfig()
    {
        try
        {
            var clientPort = (ConfigurationManager.OpenExeConfiguration(Assembly.GetExecutingAssembly().Location)).AppSettings.Settings["port"].Value;
            if (!string.IsNullOrEmpty(clientPort))
            {
                port = int.Parse(clientPort);
            }
            else
            {
                clientCacheLogger.Info("ClientPort not found in app.config. Using default port.");
            }
        }
        catch (ConfigurationErrorsException e)
        {
            clientCacheLogger.Error("Error reading app.config. Using default port.", e.InnerException);
        }
    }
    /// <summary>
    /// ICacheEvents function defination for cache Add event handling
    /// </summary>
    /// <param name="key"></param>
    public void OnItemAdded(string key)
    {
        CacheUpdated?.Invoke(this, new CacheEvent(CacheEventType.Add, key));
    }
    /// <summary>
    /// ICacheEvents functiona defination for cache Update  event handling
    /// </summary>
    /// <param name="key"></param>
    public void OnItemUpdated(string key)
    {
        CacheUpdated?.Invoke(this, new CacheEvent(CacheEventType.Update, key));
    }
    /// <summary>
    /// ICacheEvents functiona defination for cache remove event handling
    /// </summary>
    /// <param name="key"></param>
    public void OnItemRemoved(string key)
    {
        CacheUpdated?.Invoke(this, new CacheEvent(CacheEventType.Remove, key, ""));
    }
    /// <summary>
    /// To get server cache logger instance[log4net]
    /// </summary>
    /// <returns></returns>
    public static ClientLogger GetCacherLogger()
    {
        try
        {
            var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
            XmlConfigurator.Configure(logRepository, new FileInfo("log4netconfig.config"));
            return new ClientLogger(); 
        }
        catch (Exception e)
        {
            Console.WriteLine("Logger Exception: {0}", e.InnerException);
            return default;
        }
    }
}