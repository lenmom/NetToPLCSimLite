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
using System.Collections;
using System.Collections.Generic;

namespace IsoOnTcp
{
    /// <summary>
    /// Handles TPDUs of type CR (connect request) or CC (connect confirm)
    /// </summary>
    ///
    internal enum VP
    {
        /// <summary>
        /// 1000 0101 Acknowledge Time
        /// </summary>
        ACK_TIME = 0x85,

        /// <summary>
        /// 1000 0110 Residual Error Rate
        /// </summary>
        RES_ERROR = 0x86,

        /// <summary>
        /// 1000 0111 Priority
        /// </summary>
        PRIORITY = 0x87,

        /// <summary>
        /// 1000 1000 Transit Delay
        /// </summary>
        TRANSIT_DEL = 0x88,

        /// <summary>
        /// 1000 1001 Throughput
        /// </summary>
        THROUGHPUT = 0x89,

        /// <summary>
        /// 1000 1010 Subsequence Number (in AK)
        /// </summary>
        SEQ_NR = 0x8A,

        /// <summary>
        /// 1000 1011 Reassignment Time
        /// </summary>
        REASSIGNMENT = 0x8B,

        /// <summary>
        /// 1000 1100 Flow Control Confirmation (in AK)
        /// </summary>
        FLOW_CNTL = 0x8C,

        /// <summary>
        /// 1100 0000 TPDU Size
        /// </summary>
        TPDU_SIZE = 0xC0,

        /// <summary>
        /// 1100 0001 TSAP-ID / calling TSAP ( in CR/CC )
        /// </summary>
        SRC_TSAP = 0xC1,

        /// <summary>
        /// 1100 0010 TSAP-ID / called TSAP
        /// </summary>
        DST_TSAP = 0xC2,

        /// <summary>
        /// 1100 0011 Checksum
        /// </summary>
        CHECKSUM = 0xC3,

        /// <summary>
        /// 1100 0100 Version Number
        /// </summary>
        VERSION_NR = 0xC4,

        /// <summary>
        /// 1100 0101 Protection Parameters (user defined)
        /// </summary>
        PROTECTION = 0xC5,

        /// <summary>
        /// 1100 0110 Additional Option Selection
        /// </summary>
        OPT_SEL = 0xC6,

        /// <summary>
        /// 1100 0111 Alternative Protocol Classes
        /// </summary>
        PROTO_CLASS = 0xC7,

        PREF_MAX_TPDU_SIZE = 0xF0,

        INACTIVITY_TIMER = 0xF2,

        /// <summary>
        /// 1110 0000 Additional Information on Connection Clearing
        /// </summary>
        ADDICC = 0xe0
    }

    internal struct VarParam : IComparable
    {
        #region Field

        /// <summary>
        /// Parameter Code, 1 Byte
        /// </summary>
        public byte code;

        /// <summary>
        /// Parameter Length, 1 Byte
        /// </summary>
        public byte length;

        /// <summary>
        /// Parameter Value, n Byte(s)
        /// </summary>
        public byte[] value;

        #endregion

        public override bool Equals(object obj)
        {
            return this.code == (byte)obj;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public int CompareTo(object obj)
        {
            byte v = (byte)obj;
            if (this.code < v)
            {
                return -1;
            }
            else if (this.code > v)
            {
                return 1;
            }
            else
            {
                return 0;
            }
        }
    }

    internal class TPDUConnection
    {
        #region Field

        public const int MAX_TSAP_LEN = 32;

        /// <summary>
        /// Destination reference, 2 Bytes
        /// </summary>
        public ushort DstRef;

        /// <summary>
        /// Source reference, 2 Bytes
        /// </summary>
        public ushort SrcRef;

        /// <summary>
        /// Class Option, 1 Byte
        /// </summary>
        public int ClassOption;

        /// <summary>
        /// Variable part
        /// </summary>
        private ArrayList Varpart;

        /// <summary>
        /// Allowed sizes: 128(7), 256(8), 512(9), 1024(10), 2048(11) octets
        /// </summary>
        private int MaxTPDUSize;

        #endregion

        #region Constructor

        public TPDUConnection()
        { }

        public TPDUConnection(byte[] packet)
        {
            int pos;
            int li = packet[0];
            TPDU.TPDU_TYPES type = (TPDU.TPDU_TYPES)(packet[1] >> 4);

            if ((type != TPDU.TPDU_TYPES.CR) && (type != TPDU.TPDU_TYPES.CC))
            {
                throw new ApplicationException("TPDU: This can only handle CC/CR TDPUs");
            }

            if (BitConverter.IsLittleEndian)
            {
                DstRef = ByteConvert.DoReverseEndian(BitConverter.ToUInt16(packet, 2));
                SrcRef = ByteConvert.DoReverseEndian(BitConverter.ToUInt16(packet, 4));
            }
            else
            {
                DstRef = BitConverter.ToUInt16(packet, 2);
                SrcRef = BitConverter.ToUInt16(packet, 4);
            }

            ClassOption = (packet[6] & 0xf0) >> 4;
            if (ClassOption > 4)
            {
                throw new Exception("TPDU: Class option number not allowed.");
            }

            pos = 7;
            // read variable part
            Varpart = new ArrayList();
            try
            {
                while (pos < li)
                {
                    VarParam vp = new VarParam
                    {
                        code = packet[pos]
                    };
                    pos += 1;
                    vp.length = packet[pos];
                    pos += 1;
                    vp.value = new byte[vp.length];
                    Array.Copy(packet, pos, vp.value, 0, vp.length);
                    pos += vp.length;
                    Varpart.Add(vp);
                }
            }
            catch (Exception)
            {
                throw new Exception("TPDU: Error parsing variable part of CR/CC PDU.");
            }
        }

        /// <summary>
        /// Handle connect request and build a connect confirm packet
        /// </summary>
        /// <param name="LocalTsaps"></param>
        /// <param name="maxTPDUSize"></param>
        /// <param name="enableTsapCheck"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public TPDUConnection HandleConnectRequest(List<byte[]> LocalTsaps, int maxTPDUSize,
                                                   bool enableTsapCheck)
        {
            ArrayList varpart = new ArrayList();
            TPDUConnection ccpdu = new TPDUConnection();
            Random random = new Random();

            this.MaxTPDUSize = maxTPDUSize;

            ccpdu.DstRef = SrcRef;
            ccpdu.SrcRef = Convert.ToUInt16(random.Next(1, 32766));
            ccpdu.ClassOption = 0;

            // Compare Destination TSAP
            int indDstTsap = Varpart.IndexOf((byte)VP.DST_TSAP);
            if (indDstTsap < 0)
            {
                throw new Exception("TPDU: Missing destination tsap in connect request.");
            }

            if (enableTsapCheck)
            {
                bool validTsapFound = false;
                foreach (byte[] tsap in LocalTsaps)
                {
                    // Check length
                    if (((VarParam)Varpart[indDstTsap]).length == tsap.Length)
                    {
                        for (int i = 0; i < tsap.Length; i++)
                        {
                            if (((VarParam)Varpart[indDstTsap]).value[i] == tsap[i])
                            {
                                if (i == tsap.Length - 1)
                                {
                                    validTsapFound = true;
                                    break;
                                }
                            }
                            else
                            {
                                break;
                            }
                        }
                    }
                    if (validTsapFound)
                    {
                        break;
                    }
                }
                if (validTsapFound == false)
                {
                    throw new Exception("TPDU: Destination tsap mismatch.");
                }
            }

            int indSrcTsap = Varpart.IndexOf((byte)VP.SRC_TSAP);
            if (indSrcTsap < 0)
            {
                throw new Exception("TPDU: Missing source tsap in connect request.");
            }

            // Add parameters
            VarParam vp = new VarParam();
            ccpdu.Varpart = new ArrayList();

            vp.code = (byte)VP.SRC_TSAP;
            vp.length = ((VarParam)Varpart[indSrcTsap]).length;
            vp.value = new byte[vp.length];
            Array.Copy(((VarParam)Varpart[indSrcTsap]).value, vp.value, vp.length);
            ccpdu.Varpart.Add(vp);

            vp.code = (byte)VP.DST_TSAP;
            vp.length = ((VarParam)Varpart[indDstTsap]).length;
            vp.value = new byte[vp.length];
            Array.Copy(((VarParam)Varpart[indDstTsap]).value, vp.value, vp.length);
            ccpdu.Varpart.Add(vp);

            int indPduSize = Varpart.IndexOf((byte)VP.TPDU_SIZE);
            int TPDUSizeId = GetTPDUSizeIdFromOctetCount(MaxTPDUSize);
            if (indPduSize >= 0)
            {
                if (((VarParam)Varpart[indPduSize]).value[0] < TPDUSizeId)
                {
                    TPDUSizeId = ((VarParam)Varpart[indPduSize]).value[0];
                }
            }
            ccpdu.MaxTPDUSize = GetTPDUOctetCountFromSizeId(TPDUSizeId);
            vp.code = (byte)VP.TPDU_SIZE;
            vp.length = 1;
            vp.value = new byte[1];
            vp.value[0] = Convert.ToByte(TPDUSizeId);
            ccpdu.Varpart.Add(vp);

            return ccpdu;
        }

        #endregion

        #region Public Method

        /// <summary>
        /// get bytes without lenght indicator and pdu type
        /// </summary>
        /// <returns></returns>
        public byte[] GetBytes()
        {
            int len = 5; // dest-ref, src-ref, option
            foreach (VarParam v in Varpart)
            {
                len += 1 + 1 + v.length;
            }
            byte[] res = new byte[len];

            if (BitConverter.IsLittleEndian)
            {
                Buffer.BlockCopy(BitConverter.GetBytes(ByteConvert.DoReverseEndian(DstRef)), 0, res, 0, 2);
                Buffer.BlockCopy(BitConverter.GetBytes(ByteConvert.DoReverseEndian(SrcRef)), 0, res, 2, 2);
            }
            else
            {
                Buffer.BlockCopy(BitConverter.GetBytes(DstRef), 0, res, 0, 2);
                Buffer.BlockCopy(BitConverter.GetBytes(SrcRef), 0, res, 2, 2);
            }
            res[4] = (byte)ClassOption;

            int pos = 5;
            foreach (VarParam v in Varpart)
            {
                res[pos++] = v.code;
                res[pos++] = v.length;
                Buffer.BlockCopy(v.value, 0, res, pos, v.length);
                pos += v.length;
            }
            return res;
        }

        public int GetMaxTPDUSize()
        {
            return MaxTPDUSize;
        }

        public string GetStringDump()
        {
            string message;
            message = "Source reference      : " + SrcRef + Environment.NewLine;
            message += "Destination reference : " + DstRef + Environment.NewLine;
            message += "Class number          : " + ClassOption + Environment.NewLine;
            message += "---------------------" + Environment.NewLine;
            foreach (VarParam v in Varpart)
            {
                message += "Var. part code   : " + v.code + " <" + ((VP)v.code).ToString() + ">" + Environment.NewLine;
                message += "Var. part length : " + v.length + Environment.NewLine;
                message += "Var. part value  : " + Environment.NewLine;
                message += Utils.HexDump(v.value, v.length) + Environment.NewLine;
                message += "---------------------" + Environment.NewLine;
            }
            return message;
        }

        #endregion

        #region Private Method

        private int GetTPDUSizeIdFromOctetCount(int octetCount)
        {
            // Log2, but not all values allowed
            switch (octetCount)
            {
                case 128:
                    return 7;
                case 256:
                    return 8;
                case 512:
                    return 9;
                case 1024:
                    return 10;
                case 2048:
                    return 11;
                case 4096:
                    return 12;
                case 8192:
                    return 13;
                default:
                    return 7;
            }
        }

        private int GetTPDUOctetCountFromSizeId(int sizeId)
        {
            switch (sizeId)
            {
                case 7:
                    return 128;
                case 8:
                    return 256;
                case 9:
                    return 512;
                case 10:
                    return 1024;
                case 11:
                    return 2048;
                case 12:
                    return 4096;
                case 13:
                    return 8192;
                default:
                    return 0;
            }
        }

        #endregion
    }
}
