using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ReportTools.Utility
{
    public static class JsonObjectCompare
    {

        /// <summary>
        /// 只针对数据列表奥
        /// </summary>
        /// <param name="json1"></param>
        /// <param name="json2"></param>
        /// <returns></returns>
        public static List<string> GetJsonDifference(string json1,string json2) 
        {

            Object jsonObject1 = JsonConvert.DeserializeObject(json1);
            Object jsonObject2 = JsonConvert.DeserializeObject(json2);
            List<Object> objArray = new List<object>();
            objArray.Add(jsonObject1);
            objArray.Add(jsonObject2);

            List<List<string>> difference = new List<List<string>>();
            List<string> result = new List<string>();
            objArray.ForEach(item => difference.Add(JsonConvert.SerializeObject(item).Split(',').ToList()));


            result.AddRange(difference[0].Except(difference[1]).ToList());
                

            return result;
        }

    }
}
