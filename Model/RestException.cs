using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace ReportTools.Model
{
    public class RestException : Exception
    {
        public string Code { get; private set; }
        public string Message { get; private set; }
        public object Data { get; private set; }
        public string Exception { get; private set; }

        public HttpStatusCode Httpstatus { get; private set; }
        public HttpWebResponse Response { get; private set; }
        private string responseBody = null;
        public string ResponseBody
        {
            get
            {
                if (responseBody == null && this.Response != null)
                {
                    using (var streamReader = new StreamReader(this.Response.GetResponseStream()))
                        responseBody = streamReader.ReadToEnd().Trim();
                    responseBody = responseBody ?? "";
                }
                return responseBody;
            }
        }

        public RestException()
        {
        }
        public RestException(WebException we)
            : base(we.Message, we)
        {
            this.Response = we.Response as HttpWebResponse;
            if (this.Response != null)
            {
                this.Httpstatus = this.Response.StatusCode;
            }

            string returnValue = this.ResponseBody;

            if (!string.IsNullOrEmpty(returnValue) && returnValue.StartsWith("{"))
            {

                dynamic error = JsonConvert.DeserializeObject(returnValue);
                this.Code = error.code;
                this.Message = error.message;
                this.Data = error.data;
                this.Exception = error.exception;
            }
        }
    }
}
