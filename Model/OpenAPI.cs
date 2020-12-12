using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Security;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Web;
using Newtonsoft.Json;

namespace ReportTools.Model
{
    public class OpenAPI
    {
        public Credentials Credentials
        {
            get;
            set;
        }

        public string ServerUrl
        {
            get;
            private set;
        }

        public string Cookie
        {
            get
            {
                string c = string.Empty;
                foreach (KeyValuePair<string, string> item in _cookieItems)
                {
                    c += item.Key + "=" + item.Value + ";";
                }
                return c;
            }
        }

        Dictionary<string, string> _cookieItems = new Dictionary<string, string>();
        public Dictionary<string, string> CookieItems
        {
            get
            {
                return _cookieItems;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="serverUrl"></param>
        /// <param name="ce"></param>
        public OpenAPI(string serverUrl, Credentials ce)
        {
            ServerUrl = serverUrl;
            if (!ServerUrl.EndsWith("/"))
            {
                ServerUrl += "/";
            }

            Credentials = ce;

        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public dynamic ConnectTest()
        {
            return Call("Connection", null);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public dynamic GetToken()
        {
            dynamic r = null;
            try
            {
                r = Call("Authorization", new { UserName = this.Credentials.UserName, Password = this.Credentials.Password, AccountNumber = this.Credentials.AccountNumber, LoginDate = this.Credentials.LoginDate });

                this.Credentials.Access_Token = r.access_token;
            }
            catch (RestException rex)
            {
                if (rex.Code == "EXSM0004")
                {
                    this.Credentials.Access_Token = rex.Data.ToString();
                }
                throw rex;
            }

            return r;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public dynamic ReLogin()
        {
            try
            {
                return Call("Authorization/ReLogin", null);
            }
            catch (Exception e) 
            {
                throw e;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public dynamic Logout()
        {
            dynamic r = Call("Authorization/Logout", null);

            this.Credentials.Access_Token = string.Empty;

            return r;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="resource_method"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public T Call<T>(string resource_method, object args) where T : class
        {
            string postString = SerializeData(args, PostDataFormatEnum.Json);

            string returnValue = Http("POST", resource_method, postString);

            if (typeof(T) == typeof(string))
            {
                return returnValue as T;
            }

            T r = JsonConvert.DeserializeObject<T>(returnValue);

            return r;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="resource_method"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public dynamic Call(string resource_method, object args)
        {
            string postString = SerializeData(args, PostDataFormatEnum.Json);
            string returnValue = string.Empty;
            try
            {
                returnValue = Http("POST", resource_method, postString);
            }
            catch (Exception e) 
            {
                throw e;
            }

            dynamic r = JsonConvert.DeserializeObject(returnValue);

            return r;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="httpMethod">POST,GET</param>
        /// <param name="resource_method"></param>
        /// <param name="content"></param>
        /// <returns></returns>
        public string Http(string httpMethod, string resource_method, string content = "")
        {
            string ApiUrl = this.ServerUrl + resource_method;
            if (this.ServerUrl.IndexOf("?") > -1)
            {
                if (resource_method.IndexOf("?") > -1)
                {
                    ApiUrl = this.ServerUrl.Replace("?", resource_method + "&");
                }
                else
                {

                    ApiUrl = this.ServerUrl.Replace("?", resource_method + "?");
                }
            }

            string returnValue = string.Empty;

            WebClient wc = new WebClient();
            //if (ServerUrl.StartsWith("https:", StringComparison.OrdinalIgnoreCase))
            //{
            //    wc = new CertificateWebClient();
            //}
            //else
            //{
            //    wc = new WebClient();
            //}           
            //CertificateWebClient wc = new CertificateWebClient() ;

            ServicePointManager.ServerCertificateValidationCallback +=
            delegate (object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
            {
                return true;
            };


            string utcDate = string.Format("{0:R}", DateTime.Now);

            wc.Headers["Authorization"] = this.Credentials.Signature(ApiUrl, utcDate);

            wc.Headers["Content-Type"] = "application/x-www-form-urlencoded;charset=utf-8";

            if (content.StartsWith("{"))
            {
                wc.Headers["Content-Type"] = "application/json;charset=utf-8";
            }

            if (CookieItems.Count > 0)
            {
                wc.Headers["Cookie"] = Cookie;
            }

            try
            {
                if (httpMethod == "POST")
                {
                    returnValue = wc.UploadString(ApiUrl, content);
                }
                else if (httpMethod == "GET")
                {
                    ApiUrl += (ApiUrl.EndsWith("?") ? "&" : "?") + content;

                    returnValue = wc.DownloadString(ApiUrl);
                }

                string value = wc.ResponseHeaders["Set-Cookie"];
                if (!string.IsNullOrEmpty(value))
                {
                    string[] cs = value.Split(',');
                    for (int i = 0; i < cs.Length; i++)
                    {
                        string[] c = cs[i].Split(';');
                        if (c[0].IndexOf("=") == -1) continue;
                        string cookieKey = c[0].Split('=')[0];
                        string cookieValue = c[0].Split('=')[1];
                        CookieItems[cookieKey] = cookieValue;
                    }
                }
            }
            catch (WebException we)
            {
                RestException ex = new RestException(we);
                throw ex;
            }

            return returnValue;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="args"></param>
        /// <param name="dataFormat"></param>
        /// <returns></returns>
        public string SerializeData(object args, PostDataFormatEnum dataFormat)
        {
            string postString = string.Empty;
            if (args == null) return postString;

            switch (dataFormat)
            {
                case PostDataFormatEnum.FormUrlEncoded:
                    if (args is string)
                    {
                        postString = args.ToString();
                    }
                    else if (args is IDictionary<string, string>)
                    {
                        IDictionary<string, string> dic = args as IDictionary<string, string>;

                        foreach (var item in dic)
                        {
                            if (postString != string.Empty) postString += "&";
                            postString += item.Key + "=" + HttpUtility.UrlEncode(item.Value);
                        }
                    }
                    else
                    {
                        Type t = args.GetType();
                        PropertyInfo[] props = t.GetProperties();
                        foreach (var item in props)
                        {
                            if (postString != string.Empty) postString += "&";
                            postString += item.Name + "=" + HttpUtility.UrlEncode(item.GetValue(args, null) + "");
                        }
                    }
                    break;

                case PostDataFormatEnum.Json:
                    if (args is string)
                    {
                        //do nothing;
                    }
                    else
                    {
                        args = JsonConvert.SerializeObject(args);
                    }
                    postString = "_args=" + HttpUtility.UrlEncode(args.ToString(), Encoding.UTF8);
                    break;

                case PostDataFormatEnum.Mutil:
                    args = JsonConvert.SerializeObject(args);
                    postString = "_APIs=" + HttpUtility.UrlEncode(args.ToString(), Encoding.UTF8);
                    break;

                default:
                    break;
            }

            return postString;

        }
    }
}
