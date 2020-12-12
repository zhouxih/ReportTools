using ReportTools.DTO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;


namespace ReportTools.Utility
{
    public class CsvExportHelper
    {
        private static string[] _columns = { "报表名", "报表标题", "方案名", "状态", "执行次数", "总耗时", "平均耗时", "返回数据量", "错误信息" };
        public static void SaveCSV(RequestDTO dt, string fileName)
        {
            FileStream fs = new FileStream(fileName, System.IO.FileMode.Create, System.IO.FileAccess.Write);
            StreamWriter sw = new StreamWriter(fs, System.Text.Encoding.Default);
            string loopTimes = dt.LoopTimes;
            string data = string.Empty;
            List<SearchPlanInfoDTO> plans = dt.Plans;
            List<SearchPlanInfoDTO> temp = dt.Plans;


            //合并数据
            if (dt.IsGetAllData)
            {
                temp =  CombinePlan(plans);
            }

            data = string.Join(",", _columns);
            sw.WriteLine(data);
            List<string> rows = new List<string>();
            //写出各行数据
            for (int i = 0; i < plans.Count; i++)
            {
                data = string.Empty;
                rows.Add(plans[i].SearchName);
                rows.Add(plans[i].Name);
                rows.Add(plans[i].PlanName);
                rows.Add(GetStatus(plans[i].Status));
                rows.Add(loopTimes);
                string consumingTime = plans[i].ConsumingTimes.ToString();
                rows.Add(consumingTime);
                rows.Add((double.Parse(consumingTime) / double.Parse(loopTimes)).ToString());
                rows.Add(plans[i].RowsCount.ToString());
                rows.Add(plans[i].ErrorMessage);
                data = string.Join(",", rows);
                rows.Clear();
                sw.WriteLine(data);
            }
            sw.Close();
            fs.Close();
            plans.AddRange(temp);

        }
        public static List<SearchPlanInfoDTO> GetSearchPlanInfos(string filePath)
        {
            List<SearchPlanInfoDTO> result = new List<SearchPlanInfoDTO>();
            FileStream fs = new FileStream(filePath, System.IO.FileMode.Open, System.IO.FileAccess.ReadWrite);
            StreamReader sr = new StreamReader(fs);
            //跳过第一行
            string line = sr.ReadLine();
            while (line != null)
            {
                line = sr.ReadLine();
                if (line == null)
                {
                    break;
                }
                string[] temp = line.Split(',');
                result.Add(new SearchPlanInfoDTO {
                    SearchName = temp[0],
                    Name = temp[1],
                    PlanName = temp[2],
                    Status = GetDeStatus(temp[3]),
                    ConsumingTimes = double.Parse(temp[6]),
                    RowsCount = int.Parse(temp[7]),
                    ErrorMessage = temp[8]
                });
            }
            sr.Close();
            return result;
        }

        private static string GetStatus(Status status)
        {
            string response = string.Empty;
            switch (status)
            {
                case Status.Succeed:
                    response = "成功!";
                    break;
                case Status.Fail:
                    response = "失败!";
                    break;
                case Status.UnExectued:
                    response = "未执行!";
                    break;
                case Status.OnExectued:
                    response = "正在执行!";
                    break;
            }
            return response;
        }

        private static Status GetDeStatus(string status)
        {
            switch (status)
            {
                case "成功!":
                    return Status.Succeed;
                case "失败!":
                    return Status.Fail;
                case "未执行!":
                    return Status.UnExectued;
                case "正在执行!":
                    return Status.OnExectued;
                default:
                    return Status.Succeed;
            }

        }

        /// <summary>
        /// 合并父子文件
        /// </summary>
        private static List<SearchPlanInfoDTO> CombinePlan(List<SearchPlanInfoDTO> plans)
        {
            List<SearchPlanInfoDTO> fatherPlans = plans.Where(item => item.IsFather).ToList();
            List<SearchPlanInfoDTO> temp = new List<SearchPlanInfoDTO>();
            foreach (var father in fatherPlans) 
            {
                int fatherID = father.ID;
                List<SearchPlanInfoDTO> sonPlans = plans.Where(item => item.FatherID==fatherID).ToList();
                if (sonPlans.Count > 0) 
                {
                    foreach (var son in sonPlans) 
                    {
                        father.ConsumingTimes += son.ConsumingTimes;
                        father.RowsCount += son.RowsCount;
                        plans.Remove(son);
                        temp.Add(son);
                    }
                }
            }
            return temp;

        }
    }
}
