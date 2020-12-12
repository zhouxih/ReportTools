import axios from 'axios';
function Ajax(url, method = "POST", data = {}) {
    return new Promise((resolve, reject) => {
        //promise里面执行一个异步方法
        let promise;
        //如果是GET请求
        if (method === "GET") {
            promise = axios.get(url, {
                params: data
            });
        }
        //处理POST请求
        if(method==="POST"){
            promise = axios.post(url,data);
        }
        //如果是GET请求
        if (method === "DELETE") {
            promise = axios.delete(url, {
                params: data
            });
        }
        promise.then(res => {
            resolve(res.data);
        })
            //promise失败执行
            .catch(err => {
                alert("错误!");
                reject(err);
            })
        })
        
}
//请求登录
export const reqLogin = (data) => Ajax("/api/Report", "POST", data);
//请求Task状态
export const reqTaskStatus=()=>Ajax("/api/Report/RunningStatus", "GET");
//请求Task详细状态
export const reqTaskDetailStatus=(id)=>Ajax("/api/Report/RunningDetailStatus", "GET",{id});
//请求Report详细状态
export const reqReportResult=(taskid,id)=>Ajax("/api/Report/ReportResult", "GET",{taskid,id});
//请求Csv文件
export const reqCsvFile=(id)=>Ajax("/api/Report/Download", "GET",{id});
//请求删除查询任务
export const reqDeleteTaske=(id)=>Ajax("/api/Report", "DELETE",{id});
//请求唤醒查询任务
export const reqAwakeTask=()=>Ajax("/api/Report/AwakeTask", "GET");
//请求加载查询任务
export const reqHistoryTask=()=>Ajax("/api/Report/GetHistoryData", "GET");
//请求比较任务
export const reqComparaTask=(lid,rid)=>Ajax("/api/Report/ComparaTask", "GET",{lid,rid});
//请求清空查询任务
export const reqCleanTask=()=>Ajax("/api/Report/CleanTask", "GET");
//请求比较任务
export const reqComparaData=(lid,rid,searchName,planName,pageNum)=>Ajax("/api/Report/ComparaData", "GET",{lid,rid,searchName,planName,pageNum});



