using System;
using System.IO;
using System.Reflection;
using System.Text;

using log4net;
using log4net.Appender;
using log4net.Config;
using log4net.Layout;

using static log4net.Appender.FileAppender;

namespace PLCSimConnector
{
    /// <summary>
    ///     This class provide(s) functionality to log message(s) using log4net.
    /// </summary>
    [Obfuscation(ApplyToMembers = true, Exclude = false, Feature = "all", StripAfterObfuscation = true)]
    internal class Log4NetHelper : LoggerManager.ILogHelper
    {
        #region Constructor

        /// <summary>
        ///     Create(s) new instance of <c>Log4NetHelper</c> using provided parameter(s).
        /// </summary>
        public Log4NetHelper(string logDirectory)
        {
            this.MakeLog4netConfigTakeEffect(logDirectory);
            this.log = LogManager.GetLogger(typeof(Log4NetHelper));
        }

        #endregion

        #region Public Method

        /// <summary>
        ///     记录日志。
        /// </summary>
        /// <param name="level">框架日志等级。</param>
        /// <param name="message">日志内容。</param>
        public void Log(LogLevel level, object message)
        {
            switch (level)
            {
                case LogLevel.Debug:
                    this.log.DebugFormat(message.ToString());
                    break;
                case LogLevel.Info:
                    this.log.Info(message);
                    break;
                case LogLevel.Warning:
                    this.log.Warn(message);
                    break;
                case LogLevel.Error:
                    this.log.Error(message);
                    break;
                case LogLevel.Fatal:
                default:
                    this.log.Fatal(message);
                    break;
            }
        }

        #endregion

        #region Field

        private readonly log4net.ILog log;

        #endregion

        #region Private Method

        private static void ConfigureAllLogging()
        {
            try
            {
                PatternLayout patternLayout = new PatternLayout
                {
                    ConversionPattern = "%date %-5level %logger - %message%newline"
                };
                patternLayout.ActivateOptions();

                FileInfo fileInfo = new FileInfo(Assembly.GetExecutingAssembly().Location);
                // setup the appender that writes to Log\EventLoggerManager.txt
                RollingFileAppender fileAppender = new RollingFileAppender
                {
                    AppendToFile = true,
                    File = Path.Combine(Path.Combine(fileInfo.Directory.FullName, "logs"), "log"),
                    Layout = patternLayout,
                    RollingStyle = RollingFileAppender.RollingMode.Composite,
                    MaxSizeRollBackups = 10,
                    MaximumFileSize = "10MB",
                    LockingModel = new MinimalLock(),
                    StaticLogFileName = false,
                    DatePattern = "yyyy-MM-dd'.txt'"
                };

                fileAppender.ActivateOptions();


                //log4net.Repository.ILoggerRepository repository = LogManager.GetRepository();
                //IAppender[] appenders = repository.GetAppenders();
                //foreach (IAppender appender in appenders)
                //{
                //    RollingFileAppender rollingFile = appender as RollingFileAppender;
                //    if (rollingFile != null)
                //    {
                //        rollingFile.File = Path.Combine(GetAssemblyFolderFullPath(), "Logs");
                //        rollingFile.ActivateOptions();
                //    }
                //}

                BasicConfigurator.Configure(fileAppender);
            }
            catch
            {
                // Nothing to do.
            }
        }

        #endregion
    }
}