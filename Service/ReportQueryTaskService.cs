using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using ReportTools.DTO;
using ReportTools.Interface;
using ReportTools.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ReportTools.Service
{
    public class ReportQueryTaskService : IReportQueryTaskService
    {
        
        private string _appKey = "138750dc-aa1a-419a-8c92-7e1966fcd6fe";
        private string _appSecret = "ifsapb";

        public void ExcuteReportQueryTask(RequestDTO _reportQueryTask)
        {
            string loginMessage = Login(_reportQueryTask);
            //登陆失败
            if (!string.IsNullOrEmpty(loginMessage)) 
            {
                _reportQueryTask.Status = Status.Fail;
                _reportQueryTask.ErrorMessage = loginMessage;
                return;
            }
            _reportQueryTask.Status = Status.OnExectued;
            //获取所有的查询方案
            List<SearchPlanInfoDTO> plans = GetAllSearchPlanInfos(_reportQueryTask.OpenApi) as List<SearchPlanInfoDTO>;
            //如果不执行系统方案
            if (!_reportQueryTask.IsSystem)
            {
                plans = plans.Where(item => !item.IsSystem).ToList();
                plans = plans.Where(item => !item.IsPublicPlan).ToList();
            }
            else 
            {
                plans.RemoveAll(item => item.IsSystem == false && item.IsPublicPlan == true);
            }
            //所有plan的默认status都为UnExecute
            foreach (var item in plans) 
            {
                item.Status = Status.UnExectued;
            }
            _reportQueryTask.Plans = plans;
            double totalRunTime = 0;
            //如果要强行压力
            if (true)
            {
                AsyncExecuteTask(plans, _reportQueryTask);
            }
            else 
            {
                //循环执行查询报表方案
                totalRunTime = ExecuteCore(plans, _reportQueryTask);
            }
            
            
           
            _reportQueryTask.Status = Status.Succeed;
            _reportQueryTask.TotalRunTime = totalRunTime;

        }

        public IList<SearchPlanInfoDTO> GetAllSearchPlanInfos(OpenAPI openAPI)
        {
            string queryMethed = "reportQuery/GetAllSearchPlanInfos";
            string result = string.Empty;
            try
            {
                result = openAPI.Call<string>(queryMethed, null);
            }
            catch (Exception e) 
            {
                return new List<SearchPlanInfoDTO>();
            }
            return JsonConvert.DeserializeObject<IList<SearchPlanInfoDTO>>(result);
        }

        private string Login(RequestDTO _reportQueryTask) 
        {
            string strServerUrl = _reportQueryTask.ServerURL;
            if (strServerUrl.Contains("api/v2"))
            {
                strServerUrl = strServerUrl.Replace("api/v2", "api/v1");
            }
            _reportQueryTask.OpenApi = new OpenAPI(strServerUrl, new Credentials()
            {
                AppKey = _appKey,
                AppSecret = _appSecret,
                UserName = _reportQueryTask.UserName,
                Password = _reportQueryTask.Password,
                LoginDate = DateTime.Today.ToString(),
                AccountNumber = _reportQueryTask.AccountNum
            });
            dynamic r;
            try
            {
                r = _reportQueryTask.OpenApi.GetToken();
            }
            catch (RestException ex)
            {
                if (ex.Message?.Contains("对象实例") == false)
                {
                    try
                    {
                        r = _reportQueryTask.OpenApi.ReLogin();
                    }
                    catch (Exception e)
                    {
                        return ex.ResponseBody.ToString();
                    }
                }
                else
                {
                    return ex.ResponseBody.ToString();
                }
            }
            return string.Empty;
        }

        private double ExecuteCore(List<SearchPlanInfoDTO> plans, RequestDTO _reportQueryTask) 
        {
            List<SearchPlanInfoDTO> tempPlans = new List<SearchPlanInfoDTO>();
            //循环执行查询报表方案
            double totalRunTime = 0;
            string queryMethed = "reportQuery/GetReportData";
            //是否获取全部的数据
            bool isGetAllData = _reportQueryTask.IsGetAllData;
            foreach (var item in plans)
            {
                if (item.SearchName == "searchmsgtemplate"||string.IsNullOrEmpty(item.Name)) 
                {
                    continue;
                }
                item.IsFather = true;
                item.Status = Status.OnExectued;
                string queryData = string.Empty;
                //var SearchItems = new List<Object>();
                //SearchItems.Add(new
                //{
                //    ColumnName = "VoucherDate",
                //    BeginDefault = "",
                //    BeginDefaultText = "",
                //    EndDefault = "",
                //    EndDefaultText = "",
                //});
                if (item.IsSystem)
                {
                    
                    queryData = JsonConvert.SerializeObject(
                        new { request = new { ReportName = item.SearchName, PageIndex = 1,_reportQueryTask.PageSize } }
                        );
                }
                else
                {
                    queryData = JsonConvert.SerializeObject(
                        new { request = new { ReportName = item.SearchName,
                            item.PlanName,

                            PageIndex = 1, _reportQueryTask.PageSize } }
                        );
                }

                string result = string.Empty;
                int loopTimes = int.Parse(_reportQueryTask.LoopTimes);

                Stopwatch watch = Stopwatch.StartNew();
                for (int i = 0; i < loopTimes; i++)
                {
                    try
                    {
                        result = _reportQueryTask.OpenApi.Call<string>(queryMethed, queryData);
                    }
                    catch (Exception e)
                    {
                        watch.Stop();
                        item.Result = "执行异常!错误信息如下:" + e.Message;
                        item.ErrorMessage = e.Message;
                        item.ConsumingTimes = watch.Elapsed.Milliseconds;
                        totalRunTime += watch.Elapsed.Milliseconds;
                        item.Status = Status.Fail;
                        break;
                    }
                }
                watch.Stop();
                ReportOpenAPIResponse response = JsonConvert.DeserializeObject<ReportOpenAPIResponse>(result);
                if (response == null || response.DataSource == null || response.DataSource.Rows == null) 
                {
                    continue;
                }
                item.RowsCount = response.DataSource.Rows.Count;
                item.Result = result;
                item.Status = Status.Succeed;
                //plan执行异常的情况
                if (!result.Contains("\"ErrorMessage\":null"))
                {
                    item.Status = Status.Fail;
                }
                item.ConsumingTimes = watch.Elapsed.Milliseconds;
                //如果需要获取所有的数据，并且页数大于1
                if (isGetAllData) 
                {
                    if (response.Pages > 1)
                    {
                        item.TaskSessionID = response.TaskSessionID;
                        totalRunTime += ExecuteExtraTask(_reportQueryTask, item, response.Pages, tempPlans);
                    }
                }
                
                totalRunTime += watch.Elapsed.Milliseconds;

            }
            _reportQueryTask.Plans.AddRange(tempPlans);
            return totalRunTime;
        }


        private double ExecuteExtraTask(RequestDTO _reportQueryTask,SearchPlanInfoDTO plan,int pageNum,List<SearchPlanInfoDTO> tempPlans) 
        {
           //var SearchItems = new List<Object>();
           // SearchItems.Add(new
           // {
           //     ColumnName = "VoucherDate",
           //     BeginDefault = "",
           //     BeginDefaultText = "",
           //     EndDefault = "",
           //     EndDefaultText = "",
           // }); 

            //循环执行查询报表方案
            double totalRunTime = 0;
            string queryMethed = "reportQuery/GetReportData";
            string queryData = string.Empty;
            //从第二页开始请求
            for (int i = 2; i <= pageNum; i++) 
            {
                SearchPlanInfoDTO newPlan = new SearchPlanInfoDTO();
                newPlan = CopySearchPlanInfo(plan, newPlan);
                #region 拼接参数
                if (newPlan.IsSystem)
                {
                    queryData = JsonConvert.SerializeObject(
                    new { 
                        request = 
                        new { 
                        ReportName = plan.SearchName,
                        PageIndex = i,
                        _reportQueryTask.PageSize,
                            plan.PlanName,
                        newPlan.TaskSessionID
                        } 
                    }
                  );
                }
                else
                {
                    queryData = JsonConvert.SerializeObject(
                    new
                    {
                        request =
                        new
                        {
                            ReportName = plan.SearchName,
                            PageIndex = i,
                            plan.PlanName,
                            _reportQueryTask.PageSize,
                            newPlan.TaskSessionID
                        }
                    }
                  );
                }
                #endregion 
                string result = string.Empty;
                int loopTimes = int.Parse(_reportQueryTask.LoopTimes);
                Stopwatch watch = Stopwatch.StartNew();
                for (int j = 0; j < loopTimes; j++)
                {
                    try
                    {
                        result = _reportQueryTask.OpenApi.Call<string>(queryMethed, queryData);
                    }
                    catch (Exception e)
                    {
                        watch.Stop();
                        newPlan.Result = "执行异常!错误信息如下:" + e.Message;
                        newPlan.ErrorMessage = e.Message;
                        newPlan.PlanName = newPlan.PlanName + "-" + i + "Page";
                        newPlan.ConsumingTimes = watch.Elapsed.Milliseconds;
                        totalRunTime += watch.Elapsed.Milliseconds;
                        newPlan.Status = Status.Fail;
                        break;
                    }
                }
                watch.Stop();
                ReportOpenAPIResponse response = JsonConvert.DeserializeObject<ReportOpenAPIResponse>(result);
                newPlan.RowsCount = response.DataSource.Rows.Count;
                newPlan.Result = result;
                newPlan.Status = Status.Succeed;
                if (!newPlan.PlanName.Contains("Page")) 
                {
                    newPlan.PlanName = newPlan.PlanName + "-" + i + "Page";
                }
                //plan执行异常的情况
                if (!result.Contains("\"ErrorMessage\":null"))
                {
                    newPlan.Status = Status.Fail;
                }
                newPlan.ConsumingTimes = watch.Elapsed.Milliseconds;
                totalRunTime += watch.Elapsed.Milliseconds;
                //plan添加到其中
                tempPlans.Add(newPlan);
            }
            return totalRunTime;

        }


        private void AsyncExecuteTask(List<SearchPlanInfoDTO> plans, RequestDTO _reportQueryTask) 
        {
            int count = 0;
            //等待所有线程执行结束得方法
            List<ManualResetEvent> manualEvents = new List<ManualResetEvent>();
            string queryMethed = "reportQuery/GetReportData";
            foreach (var item in plans)
            {
                if (string.IsNullOrEmpty(item.Name))
                {
                    continue;
                }
                item.IsFather = true;
                item.Status = Status.OnExectued;
                string queryData = string.Empty;
                //var SearchItems = new List<Object>();
                //SearchItems.Add(new
                //{
                //    ColumnName = "VoucherDate",
                //    BeginDefault = "",
                //    BeginDefaultText = "",
                //    EndDefault = "",
                //    EndDefaultText = "",
                //});
                if (item.IsSystem)
                {

                    queryData = JsonConvert.SerializeObject(
                        new { request = new { ReportName = item.SearchName, PageIndex = 1, _reportQueryTask.PageSize } }
                        );
                }
                else
                {
                    queryData = JsonConvert.SerializeObject(
                        new
                        {
                            request = new
                            {
                                ReportName = item.SearchName,
                                item.PlanName,

                                PageIndex = 1,
                                _reportQueryTask.PageSize
                            }
                        }
                        );
                }

                string result = string.Empty;
                int loopTimes = int.Parse(_reportQueryTask.LoopTimes);
                ManualResetEvent mre = new ManualResetEvent(false);
                manualEvents.Add(mre);
                count++;
                if (count < 60)
                {
                    ThreadPool.QueueUserWorkItem((obj) =>
                    {
                        try
                        {
                            for (int i = 0; i < loopTimes; i++)
                            {
                                result = _reportQueryTask.OpenApi.Call<string>(queryMethed, queryData);
                            }
                            item.Result = result;
                            item.Status = Status.Succeed;
                            ReportOpenAPIResponse response = JsonConvert.DeserializeObject<ReportOpenAPIResponse>(result);

                        }
                        catch (Exception e)
                        {
                            item.Status = Status.Fail;
                        }

                    }, mre);
                }
                else 
                {
                    WaitHandle.WaitAll(manualEvents.ToArray());
                    manualEvents.Clear();
                    count = 0;
                   
                    
                }
              }
            WaitHandle.WaitAll(manualEvents.ToArray());

        }

        
        private List<SearchPlanInfoDTO> GetAll() 
        {
            List<SearchPlanInfoDTO> list = new List<SearchPlanInfoDTO>();
            list.Add(new SearchPlanInfoDTO {SearchName= "SA_SaleOrderSumRpt", PlanName="周夕涵" });
            list.Add(new SearchPlanInfoDTO { SearchName = "SA_SaleOrderSumRpt", PlanName = "周夕涵" });
            list.Add(new SearchPlanInfoDTO { SearchName = "SA_SaleOrderSumRpt", PlanName = "周夕涵" });
            list.Add(new SearchPlanInfoDTO { SearchName = "SA_SaleOrderSumRpt", PlanName = "周夕涵" });
            list.Add(new SearchPlanInfoDTO { SearchName = "SA_SaleOrderSumRpt", PlanName = "周夕涵" });
            return list;
        }

        /// <summary>
        /// 拷贝searchplanInfo
        /// </summary>
        /// <returns></returns>
        private SearchPlanInfoDTO CopySearchPlanInfo(SearchPlanInfoDTO src,SearchPlanInfoDTO des)
        {
            des.TaskSessionID = src.TaskSessionID;
            des.IsSystem = src.IsSystem;
            des.Name = src.Name;
            des.PlanName = src.PlanName;
            des.SearchName = src.SearchName;
            des.Status = src.Status;
            des.FatherID = src.ID;

            return des;
        }

    }
}
