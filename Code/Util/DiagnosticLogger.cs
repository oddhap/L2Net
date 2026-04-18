using System;
using System.Configuration;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace L2_login
{
    public static class DiagnosticLogger
    {
        private static readonly object LogLock = new object();
        private static bool _initialized;
        private static bool _logFirstChance;
        private static string[] _firstChanceTypes;
        [ThreadStatic]
        private static bool _isLogging;

        public static string LogPath
        {
            get
            {
                return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs", "exceptions.log");
            }
        }

        public static void Initialize()
        {
            if (_initialized)
            {
                return;
            }

            _initialized = true;
            _logFirstChance = GetBoolSetting("LogFirstChanceExceptions", true);
            _firstChanceTypes = GetSetting("LogFirstChanceExceptionTypes", "System.IO.FileNotFoundException;System.OverflowException;System.IndexOutOfRangeException").Split(';');

            AppDomain.CurrentDomain.FirstChanceException += CurrentDomain_FirstChanceException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            Application.ThreadException += Application_ThreadException;

            WriteLine("==== L2NET diagnostic logging started " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + " ====");
        }

        public static void LogException(Exception exception, string context)
        {
            if ((exception == null) || _isLogging)
            {
                return;
            }

            try
            {
                _isLogging = true;
                StringBuilder builder = new StringBuilder();
                builder.AppendLine("[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "] " + context);
                builder.AppendLine("Thread: " + Thread.CurrentThread.ManagedThreadId);
                AppendException(builder, exception);
                builder.AppendLine("CurrentStack:");
                builder.AppendLine(Environment.StackTrace);
                WriteLine(builder.ToString());
            }
            finally
            {
                _isLogging = false;
            }
        }

        private static void CurrentDomain_FirstChanceException(object sender, FirstChanceExceptionEventArgs e)
        {
            if (!_logFirstChance || (e == null) || (e.Exception == null) || !ShouldLogFirstChance(e.Exception))
            {
                return;
            }

            LogException(e.Exception, "First-chance exception");
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            LogException(e.ExceptionObject as Exception, "Unhandled AppDomain exception. IsTerminating=" + e.IsTerminating);
        }

        private static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        {
            LogException(e.Exception, "Unhandled WinForms thread exception");
        }

        private static bool ShouldLogFirstChance(Exception exception)
        {
            string exceptionType = exception.GetType().FullName;

            for (int i = 0; i < _firstChanceTypes.Length; i++)
            {
                if (string.Equals(_firstChanceTypes[i].Trim(), exceptionType, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static void AppendException(StringBuilder builder, Exception exception)
        {
            int depth = 0;

            while (exception != null)
            {
                builder.AppendLine("Exception[" + depth + "]: " + exception.GetType().FullName);
                builder.AppendLine("Message: " + exception.Message);

                FileNotFoundException fileNotFoundException = exception as FileNotFoundException;
                if ((fileNotFoundException != null) && !string.IsNullOrEmpty(fileNotFoundException.FileName))
                {
                    builder.AppendLine("FileName: " + fileNotFoundException.FileName);
                }

                builder.AppendLine("StackTrace:");
                builder.AppendLine(exception.StackTrace ?? "<no stack trace>");
                exception = exception.InnerException;
                depth++;
            }
        }

        private static bool GetBoolSetting(string key, bool defaultValue)
        {
            string value = GetSetting(key, defaultValue ? "true" : "false");
            bool parsed;
            return bool.TryParse(value, out parsed) ? parsed : defaultValue;
        }

        private static string GetSetting(string key, string defaultValue)
        {
            try
            {
                string value = ConfigurationManager.AppSettings[key];
                return string.IsNullOrEmpty(value) ? defaultValue : value;
            }
            catch
            {
                return defaultValue;
            }
        }

        private static void WriteLine(string text)
        {
            try
            {
                lock (LogLock)
                {
                    string directory = Path.GetDirectoryName(LogPath);
                    if (!Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    File.AppendAllText(LogPath, text + Environment.NewLine, Encoding.UTF8);
                }
            }
            catch
            {
                // Diagnostics should never change normal client behavior.
            }
        }
    }
}
