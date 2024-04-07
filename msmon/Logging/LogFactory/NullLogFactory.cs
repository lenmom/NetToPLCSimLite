using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace msmon
{
    /// <summary>
    /// Null log factory
    /// </summary>
    public class NullLogFactory : ILogFactory
    {
        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="NullLogFactory"/> class.
        /// </summary>
        public NullLogFactory()
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
            return new NullLog();
        }

        #endregion
    }
}
