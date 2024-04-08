using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PLCSimConnector
{
    /// <summary>
    /// Console log factory
    /// </summary>
    public class ConsoleLogFactory : ILogFactory
    {
        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="ConsoleLogFactory"/> class.
        /// </summary>
        public ConsoleLogFactory()
        {
            // Nothing to do.
        }

        #endregion

        #region Public Method

        /// <summary>
        /// Gets the log by name.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns></returns>
        public ILog GetLog(string name)
        {
            return new ConsoleLog(name);
        }

        #endregion
    }
}
