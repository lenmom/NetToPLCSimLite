/*********************************************************************
 * NetToPLCsim, Netzwerkanbindung fuer PLCSIM
 * 
 * Copyright (C) 2011-2016 Thomas Wiens, th.wiens@gmx.de
 *
 * This file is part of NetToPLCsim.
 *
 * NetToPLCsim is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as
 * published by the Free Software Foundation, either version 3 of the
 * License, or (at your option) any later version.
 /*********************************************************************/

using System;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Windows.Forms;

namespace NetToPLCSim
{
    public partial class FormLocalIpAdressDialog : Form
    {
        public FormLocalIpAdressDialog()
        {
            InitializeComponent();
        }

        public string ChosenIPaddress { get; private set; }

        private void FormLocalIpAdressDialog_Load(object sender, EventArgs e)
        {
            listBoxLocalIpAddresses.AutoSize = true;
            listBoxLocalIpAddresses.Items.Clear();

            var intf = NetworkInterface.GetAllNetworkInterfaces();
            foreach (var device in intf)
                if (device.NetworkInterfaceType.Equals(NetworkInterfaceType.Ethernet))
                    foreach (var addressInformation in device.GetIPProperties().UnicastAddresses)
                        if (addressInformation.Address.AddressFamily == AddressFamily.InterNetwork)
                            listBoxLocalIpAddresses.Items.Add(string.Format("{0} - [{1}]", addressInformation.Address, device.Description));
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            OkClicked();
        }

        private void listBoxLocalIpAddresses_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listBoxLocalIpAddresses.SelectedItem != null)
                btnOK.Enabled = true;
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            ChosenIPaddress = "";
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void listBoxLocalIpAddresses_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            OkClicked();
        }

        private void OkClicked()
        {
            if (listBoxLocalIpAddresses.SelectedItem != null)
            {
                var sel = listBoxLocalIpAddresses.SelectedItem.ToString();
                ChosenIPaddress = sel.Substring(0, sel.IndexOf(" "));
                DialogResult = DialogResult.OK;
                Close();
            }
        }
    }
}