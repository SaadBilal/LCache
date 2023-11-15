using log4net;
using System;
using System.Reflection;
namespace Log4NetSample.LogUtility
{

    /// <summary>
    /// Logger interface
    /// </summary>
    public interface ILogger
    {
        void Debug(string message);
        void Info(string message);
        void Error(string message, Exception ex);
    }

    /// <summary>
    /// Logger class
    /// </summary>
    public class ClientLogger : ILogger
    {
        private readonly ILog _logger;
        public ClientLogger()
        {
            this._logger = LogManager.GetLogger(MethodBase.GetCurrentMethod()?.DeclaringType);
        }
        public void Debug(string message)
        {
            this._logger?.Debug(message);
        }
        public void Info(string message)
        {
            this._logger?.Info(message);
        }
        public void Error(string message, Exception ex)
        {
            this._logger?.Error(message, (ex != null)?ex.InnerException:null);
        }
    }
}