using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace ReportTools.Model
{
    public class Credentials
    {
        public string UserName { get; set; }

        string psw = string.Empty;
        public string Password
        {
            get { return psw; }
            set
            {
                psw = Encrypt(value);
            }
        }
        public string AccountNumber { get; set; }
        public string LoginDate { get; set; }
        public string AppKey { get; set; }
        public string AppSecret { get; set; }

        public string Access_Token { get; set; }

        public string Signature(string uri, string utcDate)
        {

            string authParamInfo = @"{""uri"":""" + uri + @""",""access_token"":""" + Access_Token + @""",""date"":""" + utcDate + @"""}";
            string sign = string.Empty;
            string authStr = string.Empty;
            if (AppKey != "" && AppSecret != "")
            {
                HMACSHA1 hmac_sha1 = new HMACSHA1(UTF8Encoding.ASCII.GetBytes(AppSecret));
                sign = Convert.ToBase64String(hmac_sha1.ComputeHash(UTF8Encoding.UTF8.GetBytes(authParamInfo)));
                authStr = @"{""appKey"":""" + AppKey + @""",""authInfo"":""hmac-sha1 " + sign + @""",""paramInfo"":" + authParamInfo + @"}";
            }
            else
            {
                authStr = @"{""appKey"":""" + AppKey + @""",""paramInfo"":" + authParamInfo + @"}";
            }
            string encode = Convert.ToBase64String(UTF8Encoding.UTF8.GetBytes(authStr));
            return encode;
        }

        /// <summary>
        /// ToBase64String(MD5(UTF8( value )))
        /// </summary>
        /// <param name="value"></param>
        /// <param name="toBase64"></param>
        /// <returns></returns>
        static string Encrypt(string value, bool toBase64 = true)
        {
            Byte[] hashByte = new byte[] { };

            MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider();

            hashByte = md5.ComputeHash(UTF8Encoding.UTF8.GetBytes(value));

            string encode = string.Empty;

            if (toBase64)
            {
                encode = Convert.ToBase64String(hashByte);
            }
            else
            {
                StringBuilder sb = new StringBuilder();

                foreach (byte b in hashByte)
                    sb.AppendFormat("{0:x2}", b);

                encode = sb.ToString();
            }

            return encode;
        }
    }
}
