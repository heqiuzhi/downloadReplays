using Newtonsoft.Json.Linq;
using ServiceStack.OrmLite;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace DownloadDota2Replay
{
    public static class MyDB
    {
        public static IDbConnection mysqlDB;

        static MyDB()
        {
            //Set once before use (i.e. in a static constructor).
            OrmLiteConfig.DialectProvider = MySqlDialect.Provider;
            mysqlDB = ("server=127.0.0.1;uid=root;pwd=123456;database=dota2_new_stats_for_cn;").OpenDbConnection();
            //mysqlDB = ("root:123456@/dota2_new_stats_for_cn?charset=utf8&parseTime=True&loc=Local").OpenDbConnection();

            //表提前建好
            mysqlDB.CreateTableIfNotExists<Player>();

        }

        public static JObject getJsonFromUrl(string url)
        {

            try
            {
                JObject objectJson;
                var handler = new HttpClientHandler() { AutomaticDecompression = DecompressionMethods.GZip };
                using (var http = new HttpClient(handler))
                {
                    Task<HttpResponseMessage> taskGet = http.GetAsync(url);
                    taskGet.Wait();
                    var response = taskGet.Result;
                    if (response.StatusCode != HttpStatusCode.OK && response.StatusCode == HttpStatusCode.ServiceUnavailable)
                    {
                        System.Threading.Thread.Sleep(31000);
                        return getJsonFromUrl(url);
                    }
                    response.EnsureSuccessStatusCode();//正式程序不报错的好?
                    Task<string> taskRead = response.Content.ReadAsStringAsync();
                    taskRead.Wait();
                    objectJson = JObject.Parse(taskRead.Result);
                }
                return objectJson;
            }
            catch (Exception ex)
            {
                //这里想要捕获网络异常，继续尝试获取数据
                //这样写，网络连接恢复后，是可以继续获取数据的！
                return getJsonFromUrl(url);
            }
        }
    }
}
