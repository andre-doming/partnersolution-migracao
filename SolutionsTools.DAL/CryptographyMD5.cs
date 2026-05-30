using System;
using System.Security.Cryptography;
using System.Text;

namespace SolutionsTools.DAL
{
    public class CryptographyMD5
    {
        public string ReturnMD5(string password)
        {
            using (MD5 md5Hash = MD5.Create())
            {
                return ReturnHash(md5Hash, password);
            }
        }
        public bool CompareMD5(string password_DB, string password_MD5)
        {
            using (MD5 md5Hash = MD5.Create())
            {
                var password = ReturnMD5(password_MD5);
                if (VerifyHash(md5Hash, password_DB, password))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }
        private string ReturnHash(MD5 md5Hash, string input)
        {
            byte[] data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(input));

            StringBuilder sBuilder = new StringBuilder();

            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }

            return sBuilder.ToString();
        }
        private bool VerifyHash(MD5 md5Hash, string input, string hash)
        {
            StringComparer compare = StringComparer.OrdinalIgnoreCase;

            if (0 == compare.Compare(input, hash))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}