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

        internal static async Task<string> 关键字解析(string link)
        {
            if (link.Contains("mail.qq"))
            {
                return await QQ邮箱直链解析(link);
            }
            else if (link.Contains("lanzou"))
            {
                string domain = "https://" + new Regex("(\\w*\\.){2}\\w*").Match(link).Value;
                string password = new Regex("(?<=&pwd=).*").Match(link).Value;
                if (link.EndsWith("&folder"))
                {
                    return await 蓝奏云文件夹解析(domain, link.Replace($"&pwd={password}", "").Replace("&folder", ""), password.Replace("&folder", ""));
                }
                return await 蓝奏云直链解析(domain, link.Replace($"&pwd={password}", ""), password);
            }
            return "无法获取正确的链接对象...";
        }
        
        internal static async Task<string> 蓝奏云直链解析(string domain ,string Content , string password = "")
        {
            using (Web Web = new Web())
            {
                string page = await Web.Client.DownloadStringTaskAsync(Content);
                if (Msg(page, true) != "True")
                {
                    if (password == "")
                    {
                        string fn = null;
                        foreach (Match src in new Regex("/fn[^\"]*").Matches(page))
                        {
                            if (src.Length > 10)
                            {
                                fn = domain + src.Value;
                            }
                        }
                        string page1 = await Web.Client.DownloadStringTaskAsync(fn);
                        string data = $"action=downprocess&sign={new Regex("\\w*_c_c").Match(page1).Value}&ves=1";
                        string responseData = await UploadData(Content, $"{domain}/ajaxm.php", Encoding.UTF8.GetBytes(data));
                        if (responseData != "")
                        {
                            return await JsonDeserialize(responseData);
                        }
                    }
                    else if (password != "")
                    {
                        string data = $"action=downprocess&sign={new Regex("\\w*_c_c").Match(page).Value}&ves=1&p={password}";
                        byte[] postdata = Encoding.UTF8.GetBytes(data);
                        string responseData = await UploadData(Content, $"{domain}/ajaxm.php", postdata);
                        if (responseData != "")
                        {
                            return await JsonDeserialize(responseData);
                        }
                    }
                }
                else
                {
                    return $"错误：{Msg(page)}";
                }
                return "蓝奏云直链解析失败...";
            }
        }
        internal static async Task<string> 蓝奏云文件夹解析(string domain, string Content, string password = "")
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
                    string responseData = await UploadData(Content, $"{domain}/filemoreajax.php", Encoding.UTF8.GetBytes(data));
                    if (responseData != "")
                    {
                        return await JsonDeserializexFolder(domain, responseData);
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
                    string responseData = await UploadData(Content, $"{domain}/filemoreajax.php", Encoding.UTF8.GetBytes(data));
                    if (responseData != "")
                    {
                        return await JsonDeserializexFolder(domain, responseData);
                    }
                }
                return "蓝奏云文件夹解析失败...";
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

        private static async Task<string> Get(string link)
        {
            return await Task.Run(delegate
            {
                string temp;
                HttpWebRequest x = (HttpWebRequest)WebRequest.Create(link);
                x.Proxy = null;
                x.Timeout = 20000;
                x.Headers[HttpRequestHeader.AcceptLanguage] = "zh-CN,zh;q=0.9";
                x.AllowAutoRedirect = false;
                temp = x.GetResponse().Headers["Location"];
                x.Abort();
                return temp;
            });
        }

        private static string Msg(string msg, bool m = false)
        {
            if (m)
            {
                return new Regex("(?<=<div class=\"off\"><div class=\"off0\"><div class=\"off1\"></div></div>)(.*)(?=</div>)").Match(msg).Success.ToString();
            }
            return new Regex("(?<=<div class=\"off\"><div class=\"off0\"><div class=\"off1\"></div></div>)(.*)(?=</div>)").Match(msg).Value;
        }

        private static async Task<string> UploadData(string Referer, string address, byte[] postdata)
        {
            using (Web web = new Web())
            {
                web.Client.Headers[HttpRequestHeader.Referer] = Referer;
                web.Client.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
                return Encoding.UTF8.GetString(await web.Client.UploadDataTaskAsync(address, "POST", postdata));
            }
        }

        private static async Task<string> JsonDeserialize(string responseData)
        {
            Dictionary<string, string> obj= Json.Deserialize<Dictionary<string, string>>(responseData);
            if (obj["zt"] == "1")
            {
                return await Get(obj["dom"] + "/file/" + obj["url"]);
            }
            return "错误：" + obj["inf"];
        }
        private static async Task<string> JsonDeserializexFolder(string domain, string responseData)
        {
            return await Task.Run(delegate
            {

                string result = "";
                try
                {
                    var obj = Json.Deserialize<LanzouJsonFolder>(responseData);
                    if (obj.zt == "1")
                    {
                        string text = null;
                        for (int i = 0; i < obj.text.Count; i++)
                        {
                            text += $"文件名：{obj.text[i].name_all}\n大小：{obj.text[i].size}\n上传时间：{obj.text[i].time}\n链接：{domain}/{obj.text[i].id}\n\n------------------------------------\n\n";
                        }
                        result = text;
                    }
                }
                catch
                {
                    Dictionary<string, string> jsobj = Json.Deserialize<Dictionary<string, string>>(responseData);
                    if (jsobj["zt"] != "1")
                    {
                        result = "错误：" + jsobj["info"];
                    }
                }
                return result;

            });
        }

    }
    internal class Json
    {
        static readonly JavaScriptSerializer JavaScriptSerializer = new JavaScriptSerializer();
        internal static T Deserialize<T>(string JsonText)
        {
            return JavaScriptSerializer.Deserialize<T>(JsonText);
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