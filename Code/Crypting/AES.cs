using System;
using System.Security.Cryptography;
using System.IO;
using System.Text;
using System.IO.Compression;

namespace L2_login
{
    public class AES
    {
        private static AESCommon _aes = new AESCommon();

        public static string EncryptData(string data, string password, string salt)
        {
            if (data == null)
                throw new ArgumentNullException("data");
            if (password == null)
                throw new ArgumentNullException("password");
            if (salt == null)
                throw new ArgumentNullException("salt");

            byte[] encBytes = EncryptData(Encoding.UTF8.GetBytes(data), password, salt);
            return Convert.ToBase64String(encBytes);
        }

        public static string DecryptData(string data, string password, string salt)
        {
            if (data == null)
                throw new ArgumentNullException("data");
            if (password == null)
                throw new ArgumentNullException("password");
            if (salt == null)
                throw new ArgumentNullException("salt");

            byte[] encBytes = Convert.FromBase64String(data);
            byte[] decBytes = DecryptData(encBytes, password, salt);
            return Encoding.UTF8.GetString(decBytes);
        }

        public static byte[] EncryptData(byte[] data, string password, string salt)
        {
            if (data == null || data.Length == 0)
                throw new ArgumentNullException("data");
            if (password == null)
                throw new ArgumentNullException("password");
            if (salt == null)
                throw new ArgumentNullException("salt");

            return _aes.Encrypt(data, password, salt);
        }

        public static byte[] DecryptData(byte[] data, string password, string salt)
        {
            if (data == null || data.Length == 0)
                throw new ArgumentNullException("data");
            if (password == null)
                throw new ArgumentNullException("password");
            if (salt == null)
                throw new ArgumentNullException("salt");

            return _aes.Decrypt(data, password, salt);
        }

        public static byte[] Decrypt(string filename, string key, string salt)
        {
            byte[] data;
            using (FileStream fs = new FileStream(filename, FileMode.Open, FileAccess.Read))
            {
                data = new byte[fs.Length];
                fs.Read(data, 0, data.Length);
            }

            try
            {
                return Decrypt(data, key, salt);
            }
            catch (Exception e)
            {
                string err = e.Message;
                if (e.InnerException != null)
                    err += Environment.NewLine + e.InnerException.Message;

                try
                {
                    Globals.l2net_home.Add_PopUpError("failed to decrypt '" + filename + "' file data" + Environment.NewLine + err);
                }
                catch { }
                throw e;
            }
        }

        public static byte[] Decrypt(byte[] data, string key, string salt)
        {
            byte[] dec = _aes.Decrypt(data, key, salt);

            int d_len = BitConverter.ToInt32(dec, 0);

            using (MemoryStream ms = new MemoryStream(dec))
            {
                ms.Position = 4;

                using (DeflateStream compressedzipStream = new DeflateStream(ms, CompressionMode.Decompress, true))
                {
                    byte[] zdec = new byte[d_len];
                    int cnt = compressedzipStream.Read(zdec, 0, d_len);

                    byte[] result = new byte[cnt];
                    Array.ConstrainedCopy(zdec, 0, result, 0, cnt);
                    return result;
                }
            }
        }

        private class AESCommon
        {
            public byte[] Encrypt(byte[] data, string password, string salt)
            {
                using (var pdb = new PasswordDeriveBytes(password, Encoding.UTF8.GetBytes(salt)))
                {
                    using (RijndaelManaged rm = new RijndaelManaged())
                    {
                        rm.Key = pdb.GetBytes(32);
                        rm.IV = pdb.GetBytes(16);
                        rm.Padding = PaddingMode.PKCS7;
                        rm.Mode = CipherMode.CBC;

                        using (MemoryStream msEncrypt = new MemoryStream())
                        using (CryptoStream encStream = new CryptoStream(msEncrypt, rm.CreateEncryptor(), CryptoStreamMode.Write))
                        {
                            encStream.Write(data, 0, data.Length);
                            encStream.FlushFinalBlock();
                            return msEncrypt.ToArray();
                        }
                    }
                }
            }

            public byte[] Decrypt(byte[] data, string password, string salt)
            {
                try
                {
                    return DecryptWithMethod(data, password, salt, 0);
                }
                catch
                {
                    try
                    {
                        return DecryptWithMethod(data, password, salt, 1);
                    }
                    catch
                    {
                        try
                        {
                            return DecryptWithMethod(data, password, salt, 2);
                        }
                        catch
                        {
                            return DecryptWithMethod(data, password, salt, 3);
                        }
                    }
                }
            }

            private byte[] DecryptWithMethod(byte[] data, string password, string salt, int method)
            {
                if (method == 0)
                {
                    return DecryptLegacy(data, password, salt);
                }
                else if (method == 1)
                {
                    return DecryptISO10126(data, password, salt);
                }
                else if (method == 2)
                {
                    return DecryptPKCS7(data, password, salt);
                }
                else
                {
                    return DecryptZeros(data, password, salt);
                }
            }

            private byte[] DecryptPKCS7(byte[] data, string password, string salt)
            {
                using (var pdb = new PasswordDeriveBytes(password, Encoding.UTF8.GetBytes(salt)))
                {
                    using (Aes aes = Aes.Create())
                    {
                        aes.Key = pdb.GetBytes(32);
                        aes.IV = pdb.GetBytes(16);
                        aes.Padding = PaddingMode.PKCS7;

                        return DecryptWithAES(data, aes);
                    }
                }
            }

            private byte[] DecryptISO10126(byte[] data, string password, string salt)
            {
                using (var pdb = new PasswordDeriveBytes(password, Encoding.UTF8.GetBytes(salt)))
                {
                    using (Aes aes = Aes.Create())
                    {
                        aes.Key = pdb.GetBytes(32);
                        aes.IV = pdb.GetBytes(16);
                        aes.Padding = PaddingMode.ISO10126;

                        return DecryptWithAES(data, aes);
                    }
                }
            }

            private byte[] DecryptZeros(byte[] data, string password, string salt)
            {
                using (var pdb = new PasswordDeriveBytes(password, Encoding.UTF8.GetBytes(salt)))
                {
                    using (Aes aes = Aes.Create())
                    {
                        aes.Key = pdb.GetBytes(32);
                        aes.IV = pdb.GetBytes(16);
                        aes.Padding = PaddingMode.Zeros;

                        return DecryptWithAES(data, aes);
                    }
                }
            }

            private byte[] DecryptLegacy(byte[] data, string password, string salt)
            {
                using (var pdb = new PasswordDeriveBytes(password, Encoding.UTF8.GetBytes(salt)))
                {
                    using (RijndaelManaged rm = new RijndaelManaged())
                    {
                        rm.Key = pdb.GetBytes(32);
                        rm.IV = pdb.GetBytes(16);
                        rm.Padding = PaddingMode.PKCS7;
                        rm.Mode = CipherMode.CBC;

                        return DecryptWithRijndael(data, rm);
                    }
                }
            }

            private byte[] DecryptWithAES(byte[] data, Aes aes)
            {
                using (MemoryStream msDecrypt = new MemoryStream(data))
                using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, aes.CreateDecryptor(), CryptoStreamMode.Read))
                {
                    byte[] fromEncrypt = new byte[data.Length];
                    int read = csDecrypt.Read(fromEncrypt, 0, fromEncrypt.Length);
                    if (read < fromEncrypt.Length)
                    {
                        byte[] clearBytes = new byte[read];
                        Buffer.BlockCopy(fromEncrypt, 0, clearBytes, 0, read);
                        return clearBytes;
                    }
                    return fromEncrypt;
                }
            }

            private byte[] DecryptWithRijndael(byte[] data, RijndaelManaged rm)
            {
                using (MemoryStream msDecrypt = new MemoryStream(data))
                using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, rm.CreateDecryptor(), CryptoStreamMode.Read))
                {
                    byte[] fromEncrypt = new byte[data.Length];
                    int read = csDecrypt.Read(fromEncrypt, 0, fromEncrypt.Length);
                    if (read < fromEncrypt.Length)
                    {
                        byte[] clearBytes = new byte[read];
                        Buffer.BlockCopy(fromEncrypt, 0, clearBytes, 0, read);
                        return clearBytes;
                    }
                    return fromEncrypt;
                }
            }
        }
    }
}