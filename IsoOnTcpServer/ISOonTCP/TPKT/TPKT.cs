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

namespace IsoOnTcp
{
    internal class TPKT
    {
        public const int TPKT_HEADER_LENGTH = 4;
        public const int TPKT_VERSION = 3;

        /// <summary>
        /// version in TPKT header, 1 Byte
        /// </summary>
        public int Version = TPKT_VERSION;

        /// <summary>
        /// Reserved, 1 Byte
        /// </summary>
        public int Reserved = 0;

        /// <summary>
        /// Length in TPKT header, 2 Bytes
        /// </summary>
        public ushort Length;

        /// <summary>
        /// payload of TPKT packet
        /// </summary>
        private byte[] _Payload;        
        
        public byte[] Payload
        {
            get => _Payload;
            set => SetPayload(value);
        }

        /// <summary>
        /// length of TPKT payload
        /// </summary>
        public int PayloadLength;

        #region Constructor

        public TPKT()
        {
        }

        /// <summary>
        /// Extract data from TPKT packet. Length is read from packet.Length
        /// </summary>
        /// <param name="packet"></param>
        public TPKT(byte[] packet)
            : this(packet, packet.Length)
        {
        }

        /// <summary>
        /// Extract data from TPKT packet. Length is read from parameter packetLen
        /// </summary>
        /// <param name="packet"></param>
        /// <param name="packetLen"></param>
        public TPKT(byte[] packet, int packetLen)
        {
            if (packetLen < TPKT_HEADER_LENGTH)
            {
                throw new Exception("TPKT: The packet did not contain the minimum number of bytes for an TPKT header packet.");
            }

            Version = packet[0];

            if (Version != 3)
            {
                throw new Exception("TPKT: Version in header is not valid (!=3).");
            }

            if (BitConverter.IsLittleEndian)
            {
                Length = ByteConvert.DoReverseEndian(BitConverter.ToUInt16(packet, 2));
            }
            else
            {
                Length = BitConverter.ToUInt16(packet, 2);
            }

            if (Length > packetLen)
            {
                throw new Exception("TPKT: Length in header is greater than packet length.");
            }

            PayloadLength = Length - TPKT_HEADER_LENGTH;
            _Payload = new byte[PayloadLength];
            Array.Copy(packet, 4, _Payload, 0, PayloadLength);
        }

        #endregion

        #region Public Method

        public void SetPayload(byte[] data)
        {
            _Payload = new byte[data.Length];
            Array.Copy(data, 0, _Payload, 0, data.Length);
            PayloadLength = data.Length;
            Length = Convert.ToUInt16(PayloadLength + TPKT_HEADER_LENGTH);
        }

        public byte[] GetBytes()
        {
            byte[] tpkt = new byte[Length];

            tpkt[0] = Convert.ToByte(Version);
            tpkt[1] = Convert.ToByte(Reserved);

            if (BitConverter.IsLittleEndian)
            {
                Buffer.BlockCopy(BitConverter.GetBytes(ByteConvert.DoReverseEndian(Length)), 0, tpkt, 2, 2);
            }
            else
            {
                Buffer.BlockCopy(BitConverter.GetBytes(Length), 0, tpkt, 2, 2);
            }

            Array.Copy(_Payload, 0, tpkt, TPKT_HEADER_LENGTH, PayloadLength);
            return tpkt;
        }

        #endregion
    }
}
