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

using System.ComponentModel;
using System.Net;

namespace NetToPLCSim
{
    public class StationData : INotifyPropertyChanged
    {
        private bool m_Connected;
        private string m_Name;
        private IPAddress m_NetworkIpAddress;
        private IPAddress m_PlcsimIpAddress;
        private int m_PlcsimRackNumber;
        private int m_PlcsimSlotNumber;
        private string m_Status;
        private bool m_TsapCheckEnabled;

        public StationData()
        {
            m_Name = string.Empty;
            m_NetworkIpAddress = IPAddress.None;
            m_PlcsimIpAddress = IPAddress.None;
            m_PlcsimRackNumber = 0;
            m_PlcsimSlotNumber = 2;
            m_TsapCheckEnabled = false;
            m_Status = StationStatus.READY.ToString();
        }

        public StationData(string name, IPAddress networkIpAddress, IPAddress plcsimIpAddress, int rack, int slot, bool tsapCheckEnabled)
        {
            m_Name = name;
            m_NetworkIpAddress = networkIpAddress;
            m_PlcsimIpAddress = plcsimIpAddress;
            m_PlcsimRackNumber = rack;
            m_PlcsimSlotNumber = slot;
            m_TsapCheckEnabled = tsapCheckEnabled;
            m_Status = StationStatus.READY.ToString();
        }

        public string Name
        {
            get => m_Name;
            set
            {
                m_Name = value;
                NotifyPropertyChanged("Name");
            }
        }

        public IPAddress NetworkIpAddress
        {
            get => m_NetworkIpAddress;
            set
            {
                m_NetworkIpAddress = value;
                NotifyPropertyChanged("NetworkIpAddress");
            }
        }

        public IPAddress PlcsimIpAddress
        {
            get => m_PlcsimIpAddress;
            set
            {
                m_PlcsimIpAddress = value;
                NotifyPropertyChanged("PlcsimIpAddress");
            }
        }

        public int PlcsimRackNumber
        {
            get => m_PlcsimRackNumber;
            set
            {
                m_PlcsimRackNumber = value;
                NotifyPropertyChanged("PlcsimRackSlot");
            }
        }

        public int PlcsimSlotNumber
        {
            get => m_PlcsimSlotNumber;
            set
            {
                m_PlcsimSlotNumber = value;
                NotifyPropertyChanged("PlcsimRackSlot");
            }
        }

        public string PlcsimRackSlot => m_PlcsimRackNumber + "/" + m_PlcsimSlotNumber;

        public bool TsapCheckEnabled
        {
            get => m_TsapCheckEnabled;
            set
            {
                m_TsapCheckEnabled = value;
                NotifyPropertyChanged("TsapCheckEnabled");
            }
        }

        public bool Connected
        {
            get => m_Connected;
            set
            {
                m_Connected = value;
                NotifyPropertyChanged("Connected");
            }
        }

        public string Status
        {
            get => m_Status;
            set
            {
                m_Status = value;
                NotifyPropertyChanged("Status");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string name)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(name));
            }
        }

        public StationData ShallowCopy()
        {
            return (StationData)MemberwiseClone();
        }
    }
}