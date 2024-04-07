using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace msmon
{
    public static class Utility
    {
        /// <summary>
        ///     字符相关的实用函数。
        /// </summary>
        public static class Text
        {
            [ThreadStatic] private static StringBuilder s_CachedStringBuilder;

            /// <summary>
            ///     获取格式化字符串。
            /// </summary>
            /// <param name="format">字符串格式。</param>
            /// <param name="arg0">字符串参数 0。</param>
            /// <returns>格式化后的字符串。</returns>
            public static string Format(string format, object arg0)
            {
                if (format == null)
                {
                    throw new Exception("Format is invalid.");
                }

                CheckCachedStringBuilder();
                s_CachedStringBuilder.Length = 0;
                s_CachedStringBuilder.AppendFormat(format, arg0);
                return s_CachedStringBuilder.ToString();
            }

            /// <summary>
            ///     获取格式化字符串。
            /// </summary>
            /// <param name="format">字符串格式。</param>
            /// <param name="arg0">字符串参数 0。</param>
            /// <param name="arg1">字符串参数 1。</param>
            /// <returns>格式化后的字符串。</returns>
            public static string Format(string format, object arg0, object arg1)
            {
                if (format == null)
                {
                    throw new Exception("Format is invalid.");
                }

                CheckCachedStringBuilder();
                s_CachedStringBuilder.Length = 0;
                s_CachedStringBuilder.AppendFormat(format, arg0, arg1);
                return s_CachedStringBuilder.ToString();
            }

            /// <summary>
            ///     获取格式化字符串。
            /// </summary>
            /// <param name="format">字符串格式。</param>
            /// <param name="arg0">字符串参数 0。</param>
            /// <param name="arg1">字符串参数 1。</param>
            /// <param name="arg2">字符串参数 2。</param>
            /// <returns>格式化后的字符串。</returns>
            public static string Format(string format, object arg0, object arg1, object arg2)
            {
                if (format == null)
                {
                    throw new Exception("Format is invalid.");
                }

                CheckCachedStringBuilder();
                s_CachedStringBuilder.Length = 0;
                s_CachedStringBuilder.AppendFormat(format, arg0, arg1, arg2);
                return s_CachedStringBuilder.ToString();
            }

            /// <summary>
            ///     获取格式化字符串。
            /// </summary>
            /// <param name="format">字符串格式。</param>
            /// <param name="args">字符串参数。</param>
            /// <returns>格式化后的字符串。</returns>
            public static string Format(string format, params object[] args)
            {
                if (format == null)
                {
                    throw new Exception("Format is invalid.");
                }

                if (args == null)
                {
                    throw new Exception("Args is invalid.");
                }

                CheckCachedStringBuilder();
                s_CachedStringBuilder.Length = 0;
                s_CachedStringBuilder.AppendFormat(format, args);
                return s_CachedStringBuilder.ToString();
            }

            private static void CheckCachedStringBuilder()
            {
                if (s_CachedStringBuilder == null)
                {
                    s_CachedStringBuilder = new StringBuilder(1024);
                }
            }
        }

        public static byte[] EncryptStringToBytes(string plainText, byte[] Key, byte[] IV)
        {
            if (plainText == null || plainText.Length <= 0)
            {
                throw new ArgumentNullException("plainText");
            }

            if (Key == null || Key.Length <= 0)
            {
                throw new ArgumentNullException("Key");
            }

            if (IV == null || IV.Length <= 0)
            {
                throw new ArgumentNullException("IV");
            }

            byte[] encrypted;

            using (AesCryptoServiceProvider aesAlg = new AesCryptoServiceProvider())
            {
                aesAlg.Key = Key;
                aesAlg.IV = IV;

                ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                        {
                            swEncrypt.Write(plainText);
                        }
                        encrypted = msEncrypt.ToArray();
                    }
                }
            }

            return encrypted;
        }


        public static string DecryptStringFromBytes(byte[] cipherText, byte[] Key, byte[] IV)
        {
            if (cipherText == null || cipherText.Length <= 0)
            {
                throw new ArgumentNullException("cipherText");
            }

            if (Key == null || Key.Length <= 0)
            {
                throw new ArgumentNullException("Key");
            }

            if (IV == null || IV.Length <= 0)
            {
                throw new ArgumentNullException("IV");
            }

            string plaintext = null;

            using (AesCryptoServiceProvider aesAlg = new AesCryptoServiceProvider())
            {
                aesAlg.Key = Key;
                aesAlg.IV = IV;

                ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                using (MemoryStream msDecrypt = new MemoryStream(cipherText))
                {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                        {
                            plaintext = srDecrypt.ReadToEnd();
                        }
                    }
                }
            }

            return plaintext;
        }
    }
}