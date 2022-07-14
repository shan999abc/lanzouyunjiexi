using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace TEST
{
    internal class Download
    {
        public class LanzouJsonFolderList
        {
            public string id { get; set; }
            public string name_all { get; set; }
            public string size { get; set; }
            public string time { get; set; }
        }
        public class LanzouJsonFolder
        {
            public string zt { get; set; }
            public string info { get; set; }
            public List<LanzouJsonFolderList> text { get; set; }
        }

        internal static string Get(string link)
        {
            string temp;
            HttpWebRequest x = (HttpWebRequest)WebRequest.Create(link);
            x.Timeout = 3000;
            x.Headers[HttpRequestHeader.AcceptLanguage] = "zh-CN,zh;q=0.9";
            x.AllowAutoRedirect = false;
            temp = x.GetResponse().Headers["Location"];
            x.Abort();
            return temp;
        }
        internal static async Task<string> 关键字解析(string link)
        {
            if (link.Contains("mail.qq"))
            {
                return await QQ邮箱直链解析(link);
            }
            else if (link.Contains("lanzou"))
            {
                string password = new Regex($"(?<=&pwd=).+").Match(link).Value;
                if (link.EndsWith("&folder"))
                {
                    return await 蓝奏云文件夹解析(link.Replace($"&pwd={password}","").Replace("&folder",""),password.Replace("&folder",""));
                }
                return await 蓝奏云直链解析(link.Replace($"&pwd={password}",""),password);
            }
            return "无法获取正确的链接对象...";
        }
        internal static async Task<string> 蓝奏云文件夹解析(string Content, string password = "")
        {
            using (Web Web = new Web())
            {
                string page = await Web.Client.DownloadStringTaskAsync(Content);
                if (password == "")
                {
                    string t = new Regex("(?<='t':)(.*)(?=,)").Match(page).Value;
                    string k = new Regex("(?<='k':)(.*)(?=,)").Match(page).Value;
                    string lx = new Regex("(?<=lx':)(.*)(?=,)").Match(page).Value;
                    string fid = new Regex("(?<=fid':)(.*)(?=,)").Match(page).Value;
                    string uid = new Regex("(?<=uid':')(.*)(?=')").Match(page).Value;
                    string t1 = new Regex($"(?<={t} = ')(.*)(?=')").Match(page).Value;
                    string k1 = new Regex($"(?<={k} = ')(.*)(?=')").Match(page).Value;
                    string data = $"lx={lx}&fid={fid}&uid={uid}&pg=1&rep=0&t={t1}&k={k1}&up=1&vip=0&webfoldersign=";
                    byte[] postdata = Encoding.UTF8.GetBytes(data);
                    Web.Client.Headers[HttpRequestHeader.Referer] = Content;
                    Web.Client.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
                    byte[] responseData = await Web.Client.UploadDataTaskAsync("https://lanzoux.com/filemoreajax.php", "POST", postdata);
                    if (Encoding.UTF8.GetString(responseData) != "")
                    {
                        try
                        {
                            var jsobj = new JavaScriptSerializer().Deserialize<LanzouJsonFolder>(Encoding.UTF8.GetString(responseData));
                            if (jsobj.zt == "1")
                            {
                                string text = null;
                                for (int i = 0; i < jsobj.text.Count; i++)
                                {
                                    text += $"链接：https://www.lanzoux.com/{jsobj.text[i].id}\n文件名：{jsobj.text[i].name_all}\n大小：{jsobj.text[i].size}\n上传时间：{jsobj.text[i].time}\n";
                                }
                                return text;
                            }
                        }
                        catch
                        {
                            Dictionary<string, string> jsobj = new JavaScriptSerializer().Deserialize<Dictionary<string, string>>(Encoding.UTF8.GetString(responseData));
                            if (jsobj["zt"] != "1")
                            {
                                return "错误：" + jsobj["info"];
                            }
                        }
                    }
                }
                else if (password != "")
                {
                    string t = new Regex("(?<='t':)(.*)(?=,)").Match(page).Value;
                    string k = new Regex("(?<='k':)(.*)(?=,)").Match(page).Value;
                    string lx = new Regex("(?<=lx':)(.*)(?=,)").Match(page).Value;
                    string fid = new Regex("(?<=fid':)(.*)(?=,)").Match(page).Value;
                    string uid = new Regex("(?<=uid':')(.*)(?=')").Match(page).Value;
                    string t1 = new Regex($"(?<={t} = ')(.*)(?=')").Match(page).Value;
                    string k1 = new Regex($"(?<={k} = ')(.*)(?=')").Match(page).Value;
                    string data = $"lx={lx}&fid={fid}&uid={uid}&pg=1&rep=0&t={t1}&k={k1}&up=1&ls=1&pwd={password}";
                    byte[] postdata = Encoding.UTF8.GetBytes(data);
                    Web.Client.Headers[HttpRequestHeader.Referer] = Content;
                    Web.Client.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
                    byte[] responseData = await Web.Client.UploadDataTaskAsync("https://lanzoux.com/filemoreajax.php", "POST", postdata);
                    if (Encoding.UTF8.GetString(responseData) != "")
                    {
                        try
                        {
                            var jsobj = new JavaScriptSerializer().Deserialize<LanzouJsonFolder>(Encoding.UTF8.GetString(responseData));
                            if (jsobj.zt == "1")
                            {
                                string text = null;
                                for (int i = 0; i < jsobj.text.Count; i++)
                                {
                                    text += $"链接：https://www.lanzoux.com/{jsobj.text[i].id}\n文件名：{jsobj.text[i].name_all}\n大小：{jsobj.text[i].size}\n上传时间：{jsobj.text[i].time}\n";
                                }
                                return text;
                            }
                        }
                        catch
                        {
                            Dictionary<string, string> jsobj = new JavaScriptSerializer().Deserialize<Dictionary<string, string>>(Encoding.UTF8.GetString(responseData));
                            if (jsobj["zt"] != "1")
                            {
                                return "错误：" + jsobj["info"];
                            }
                        }
                        
                    }
                }
                return "蓝奏云文件夹解析失败...";
            }
        }
        
        internal static async Task<string> 蓝奏云直链解析(string Content , string password = "")
        {
            using (Web Web = new Web())
            {
                string page = await Web.Client.DownloadStringTaskAsync(Content);
                if (!new Regex("(?<=<div class=\"off\"><div class=\"off0\"><div class=\"off1\"></div></div>)(.*)(?=</div>)").Match(page).Success)
                {
                    if (password == "")
                    {
                        string fn = null;
                        foreach (Match src in new Regex("/fn[^\"]+").Matches(page))
                        {
                            if (src.Length > 10)
                            {
                                fn = "https://www.lanzoux.com" + src.Value;
                            }
                        }
                        string page1 = await Web.Client.DownloadStringTaskAsync(fn);
                        string data = $"action=downprocess&sign={new Regex("[0-9a-zA-Z_]{70,}_c_c").Match(page1).Value}&ves=1";
                        byte[] postdata = Encoding.UTF8.GetBytes(data);
                        Web.Client.Headers[HttpRequestHeader.Referer] = Content;
                        Web.Client.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
                        byte[] responseData = await Web.Client.UploadDataTaskAsync("https://www.lanzoux.com/ajaxm.php", "POST", postdata);
                        if (Encoding.UTF8.GetString(responseData) != "")
                        {
                            Dictionary<string, string> jsobj = new JavaScriptSerializer().Deserialize<Dictionary<string, string>>(Encoding.UTF8.GetString(responseData));
                            if (jsobj["zt"] == "1")
                            {
                                return Get(jsobj["dom"] + "/file/" + jsobj["url"]);
                            }
                            return "错误：" + jsobj["inf"];
                        }
                    }
                    else if (password != "")
                    {
                        string data = $"action=downprocess&sign={new Regex("[0-9a-zA-Z_]{70,}_c_c").Match(page).Value}&ves=1&p={password}";
                        byte[] postdata = Encoding.UTF8.GetBytes(data);
                        Web.Client.Headers[HttpRequestHeader.Referer] = Content;
                        Web.Client.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
                        byte[] responseData = await Web.Client.UploadDataTaskAsync("https://www.lanzoux.com/ajaxm.php", "POST", postdata);
                        if (Encoding.UTF8.GetString(responseData) != "")
                        {
                            Dictionary<string, string> jsobj = new JavaScriptSerializer().Deserialize<Dictionary<string, string>>(Encoding.UTF8.GetString(responseData));
                            if (jsobj["zt"] == "1")
                            {
                                return Get(jsobj["dom"] + "/file/" + jsobj["url"]);
                            }
                            return "错误：" + jsobj["inf"];
                        }
                    }
                }
                else
                {
                    return $"错误：{new Regex("(?<=<div class=\"off\"><div class=\"off0\"><div class=\"off1\"></div></div>)(.*)(?=</div>)").Match(page).Value}";
                }
                return "蓝奏云直链解析失败...";
            }
        }
        internal static async Task<string> QQ邮箱直链解析(string Content)
        {
            using (Web Web = new Web())
            {
                string page = await Web.Client.DownloadStringTaskAsync(Content);
                foreach (Match link in new Regex("http[^\"]+").Matches(page))
                {
                    if (link.Value.Contains("download.ftn.qq.com"))
                    {
                        return link.Value;
                    }
                }
                return "QQ邮箱直链解析失败...";
            }
        }
    }
    public class Web : IDisposable
    {
        public WebClient Client { get; private set; }
        public Web()
        {
            Client = new WebClient
            {
                Proxy = null,
                Encoding = Encoding.UTF8,
            };
            Client.Headers[HttpRequestHeader.UserAgent] = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/99.9.9999.99 Safari/537.36 Edg/99.9.9999.99";
            Client.Headers[HttpRequestHeader.AcceptLanguage] = "zh-CN,zh;q=0.9";
        }
        public void Dispose()
        {
            Dispose(true);
        }
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                Client?.Dispose();
            }
        }

    }
}