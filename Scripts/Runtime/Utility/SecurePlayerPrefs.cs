using UnityEngine;
using System.Text;
using System.Security.Cryptography;

namespace Assetlayer.UtilityFunctions
{

    public static class SecurePlayerPrefs
    {
        private static readonly string encryptionKey = "YourSecretEncryptionKeyHerew5z75";

        public static void SetSecureString(string key, string value)
        {
            string encryptedValue = Encrypt(value, encryptionKey);
            PlayerPrefs.SetString(key, encryptedValue);
        }

        public static string GetSecureString(string key, string defaultValue = "")
        {
            if (PlayerPrefs.HasKey(key))
            {
                string encryptedValue = PlayerPrefs.GetString(key);
                return Decrypt(encryptedValue, encryptionKey);
            }
            return defaultValue;
        }

        public static void RemoveSecureString(string key)
        {
            if (PlayerPrefs.HasKey(key))
            {
                PlayerPrefs.DeleteKey(key);
            }
        }

        private static string Encrypt(string toEncrypt, string key)
        {
            byte[] keyArray = UTF8Encoding.UTF8.GetBytes(key);
            byte[] toEncryptArray = UTF8Encoding.UTF8.GetBytes(toEncrypt);

            using (RijndaelManaged rDel = new RijndaelManaged())
            {
                rDel.Key = keyArray;
                rDel.Mode = CipherMode.ECB;
                rDel.Padding = PaddingMode.PKCS7;

                ICryptoTransform cTransform = rDel.CreateEncryptor();
                byte[] resultArray = cTransform.TransformFinalBlock(toEncryptArray, 0, toEncryptArray.Length);

                return System.Convert.ToBase64String(resultArray, 0, resultArray.Length);
            }
        }

        private static string Decrypt(string toDecrypt, string key)
        {
            byte[] keyArray = UTF8Encoding.UTF8.GetBytes(key);
            byte[] toDecryptArray = System.Convert.FromBase64String(toDecrypt);

            using (RijndaelManaged rDel = new RijndaelManaged())
            {
                rDel.Key = keyArray;
                rDel.Mode = CipherMode.ECB;
                rDel.Padding = PaddingMode.PKCS7;

                ICryptoTransform cTransform = rDel.CreateDecryptor();
                byte[] resultArray = cTransform.TransformFinalBlock(toDecryptArray, 0, toDecryptArray.Length);

                return UTF8Encoding.UTF8.GetString(resultArray);
            }
        }
    }
}
