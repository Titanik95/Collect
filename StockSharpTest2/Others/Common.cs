using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Security;
using System.Security.Cryptography;
using System.Text;


namespace Collect
{
    class Common
    {
        public static string GetConnectionString(DataStorage type = 0)
        {
            string connectionStringName = "";
            switch (type)
            {
                case DataStorage.Local:
                    connectionStringName = "ConnectionStringLocal";
                    break;
                case DataStorage.Cloud:
                    connectionStringName = "ConnectionStringCloud";
                    break;
            }
            return ConfigurationManager.ConnectionStrings[connectionStringName].ToString();
        }

        public static byte[] EncryptPassword(string pass)
        {
            try
            {
                using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(3072))
                {
                    using (StreamWriter sw = new StreamWriter(Properties.Resources.KeyFileName))
                    {
                        sw.WriteLine(rsa.ToXmlString(true));
                    }

                    return rsa.Encrypt(Encoding.Unicode.GetBytes(pass), false);
                }
            }
            catch
            {
                return null;       
            }
        }

        public static string DecryptPassword(byte[] pass)
        {
            try
            {
                using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(3072))
                {
                    string key;
                    using (StreamReader sr = new StreamReader(Properties.Resources.KeyFileName))
                    {
                        key = sr.ReadLine();
                    }

                    rsa.FromXmlString(key);
                    return Encoding.Unicode.GetString(rsa.Decrypt(pass, false));
                }
            }
            catch
            {
                return "";
            }
        }
    }
}
