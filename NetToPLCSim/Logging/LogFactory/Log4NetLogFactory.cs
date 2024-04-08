using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;

using log4net.Config;

namespace PLCSimConnector
{
    /// <summary>
    /// Log4NetLogFactory
    /// </summary>
    public class Log4NetLogFactory: ILogFactory
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Log4NetLogFactory"/> class.
        /// </summary>
        public Log4NetLogFactory()
            : this(string.Empty)
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Log4NetLogFactory"/> class.
        /// </summary>
        /// <param name="logDirectory">The log directory full path.</param>
        public Log4NetLogFactory(string logDirectory)
        //: base(log4netConfig)
        {
            //if (!IsSharedConfig)
            //{
            //    log4net.Config.XmlConfigurator.Configure(new FileInfo(ConfigFile));
            //}
            //else
            //{
            //    //Disable Performance logger
            //    var xmlDoc = new XmlDocument();
            //    xmlDoc.Load(ConfigFile);
            //    var docElement = xmlDoc.DocumentElement;
            //    var perfLogNode = docElement.SelectSingleNode("logger[@name='Performance']");
            //    if (perfLogNode != null)
            //        docElement.RemoveChild(perfLogNode);
            //    log4net.Config.XmlConfigurator.Configure(docElement);
            //}
            this.MakeLog4netConfigTakeEffect(logDirectory);
        }

        /// <summary>
        /// Gets the log by name.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns></returns>
        public virtual ILog GetLog(string name)
        {
            return new Log4NetLog(log4net.LogManager.GetLogger(name));
        }
    }
}
