using Newtonsoft.Json;
using ReportTools.DTO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ReportTools.Utility
{
    public class FileHelper
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="request">Task</param>
        /// <param name="path">文件夹位置</param>
        public static void Serialize(RequestDTO request,string path) 
        {
            //创建文件夹
            Directory.CreateDirectory(Path.Combine(path, request.ID));
            Dictionary<string, List<SearchPlanInfoDTO>> dic = new Dictionary<string, List<SearchPlanInfoDTO>>();
            foreach (var item in request.Plans) 
            {
                if (!dic.ContainsKey(item.SearchName))
                {
                    List<SearchPlanInfoDTO> temp = new List<SearchPlanInfoDTO>();
                    temp.Add(item);
                    dic.Add(item.SearchName, temp);
                }
                else 
                {
                    var temp = dic[item.SearchName];
                    temp.Add(item);
                }     
            }
            request.FileAddress = Path.Combine(path, request.ID);
            //开始序列化到本地
            foreach (var item in dic.Keys) 
            {
                Directory.CreateDirectory(Path.Combine(path, request.ID, item));
                List<SearchPlanInfoDTO> temp = dic[item];
                foreach(var plan in temp) 
                {
                    plan.PlanName = plan.PlanName.Replace(' ','-');
                    plan.PlanName = plan.PlanName.Replace('/', '-');
                    plan.PlanName = plan.PlanName.Replace(':', '-');
                    string filePath = Path.Combine(path, request.ID, item, plan.PlanName+".json");
                    File.WriteAllText(filePath,plan.Result);
                    //序列化结束之后将结果去掉避免消耗内存
                    plan.Result = string.Empty;
                }

            }

            //将方案放入文件中
            string taskFilePath = Path.Combine(path, request.ID, "ResultFile.json");
            File.WriteAllText(taskFilePath, JsonConvert.SerializeObject(request));

        }

        public static void DeleteDirectory(string filePath)
        {   
            //判断文件夹是否还存在
            if (Directory.Exists(filePath))
            {
                //去除文件夹和子文件的只读属性
                //去除文件夹的只读属性
                System.IO.DirectoryInfo fileInfo = new DirectoryInfo(filePath);
                fileInfo.Attributes = FileAttributes.Normal & FileAttributes.Directory;

                foreach (string f in Directory.GetFileSystemEntries(filePath))
                {
                    if (File.Exists(f))
                    {
                        //去除文件的只读属性
                        System.IO.File.SetAttributes(f, System.IO.FileAttributes.Normal);
                        //如果有子文件删除文件
                        File.Delete(f);
                    }
                    else
                    {
                        //循环递归删除子文件夹
                        DeleteDirectory(f);
                    }
                }
                //删除空文件夹
                Directory.Delete(filePath);
            }

        }

        public static object DeSerialize(string filePath) 
        {
            if (!File.Exists(filePath)) 
            {
                return null;
            }
            string json = File.ReadAllText(filePath);
            Object o = JsonConvert.DeserializeObject(json);
            return o;
        }

    }
}
