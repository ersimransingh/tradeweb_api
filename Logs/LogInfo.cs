using System;
using System.IO;

namespace TradeWeb.API.Logs
{
    /// <summary>
    /// Minimal file logger. Writes to Logs/ErrorLog_yyyyMMdd.txt under the app base directory.
    /// </summary>
    public static class LogInfo
    {
        private static readonly object _lock = new object();

        public static void WriteErrorLog(string message)
        {
            try
            {
                var dir = Path.Combine(AppContext.BaseDirectory, "Logs");
                Directory.CreateDirectory(dir);
                var file = Path.Combine(dir, $"ErrorLog_{DateTime.Now:yyyyMMdd}.txt");
                var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} : {message}{Environment.NewLine}";
                lock (_lock)
                {
                    File.AppendAllText(file, line);
                }
            }
            catch
            {
                // Logging must never break the request.
            }
        }

        public static void WriteErrorLog(Exception ex)
        {
            if (ex == null) return;
            WriteErrorLog(ex.ToString());
        }
    }
}
