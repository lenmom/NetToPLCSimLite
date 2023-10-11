using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PLCSimConnector
{
    internal class FormMainPresenter
    {
        #region Field

        private readonly IFormMainView formMainView;

        #endregion

        #region Constructor

        internal FormMainPresenter(IFormMainView formMainView)
        {
            this.formMainView = formMainView;
            this.formMainView.OnLoad += FormMainView_OnLoad;
        }

        private void FormMainView_OnLoad(string[] args)
        {

        }

        #endregion
    }
}
