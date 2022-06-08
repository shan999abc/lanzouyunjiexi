using Newtonsoft.Json.Linq;
using System;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace TEST
{
    internal class Download
    {
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
                    JObject js = JObject.Parse(Encoding.UTF8.GetString(responseData));
                    if (js["zt"].ToString() == "1")
                    {
                        JArray array = JArray.Parse(js["text"].ToString());
                        string text = null;
                        for (int i = 0; i < array.Count; i++)
                        {
                            JObject js1 = JObject.Parse(array[i].ToString());
                            text += $"链接：https://www.lanzoux.com/{js1["id"]}\n文件名：{js1["name_all"]}\n大小：{js1["size"]}\n上传时间：{js1["time"]}\n";
                        }
                        return text;
                    }
                    else
                    {
                        return "错误：" + js["info"].ToString();
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
                    JObject js = JObject.Parse(Encoding.UTF8.GetString(responseData));
                    if (js["zt"].ToString() == "1")
                    {
                        JArray array = JArray.Parse(js["text"].ToString());
                        string text = null;
                        for (int i = 0; i < array.Count; i++)
                        {
                            JObject js1 = JObject.Parse(array[i].ToString());
                            text += $"链接：https://www.lanzoux.com/{js1["id"]}\n文件名：{js1["name_all"]}\n大小：{js1["size"]}\n上传时间：{js1["time"]}\n";
                        }
                        return text;
                    }
                    else
                    {
                        return "错误：" + js["info"].ToString();
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
                        string data = $"action=downprocess&signs=?ctdf&sign={new Regex("(?<=sign':')(.*)(?=',)").Match(page1).Value}&ves=1&websign=&websignkey=aNA1";
                        byte[] postdata = Encoding.UTF8.GetBytes(data);
                        Web.Client.Headers[HttpRequestHeader.Referer] = Content;
                        Web.Client.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
                        byte[] responseData = await Web.Client.UploadDataTaskAsync("https://www.lanzoux.com/ajaxm.php", "POST", postdata);
                        JObject js = JObject.Parse(Encoding.UTF8.GetString(responseData));
                        if (js["zt"].ToString() == "1")
                        {
                            return Get($"{js["dom"]}/file/{js["url"]}");
                        }
                        else
                        {
                            return "错误：" + js["inf"].ToString();
                        }
                    }
                    else if (password != "")
                    {
                        string data = new Regex("(?<=data : ')(.*)(?=')").Match(page).Value + password;
                        byte[] postdata = Encoding.UTF8.GetBytes(data);
                        Web.Client.Headers[HttpRequestHeader.Referer] = Content;
                        Web.Client.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
                        byte[] responseData = await Web.Client.UploadDataTaskAsync("https://www.lanzoux.com/ajaxm.php", "POST", postdata);
                        JObject js = JObject.Parse(Encoding.UTF8.GetString(responseData));
                        if (js["zt"].ToString() == "1")
                        {
                            return Get($"{js["dom"]}/file/{js["url"]}");
                        }
                        else
                        {
                            return "错误：" + js["inf"].ToString();
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
