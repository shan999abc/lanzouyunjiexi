using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace TEST
{
    internal class Download
    {
        public class LanzouJson
        {
            public string Dom { get; set; }
            public string Url { get; set; }
        }
        public class Text
        {
            public string Icon { get; set; }
            public string T { get; set; }
            public string Id { get; set; }
            public string Name_all { get; set; }
            public string Size { get; set; }
            public string Time { get; set; }
            public string Duan { get; set; }
            public string P_ico { get; set; }
        }

        public class LanzouJsonFilelist
        {
            public List<Text> Text { get; set; }
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
        internal static async Task<string> 蓝奏云文件夹解析(string Content, string password = null)
        {
            using (Web Web = new Web())
            {
                string page = await Web.Client.DownloadStringTaskAsync(Content);
                if (password == null)
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
                    LanzouJsonFilelist js = JsonConvert.DeserializeObject<LanzouJsonFilelist>(Encoding.UTF8.GetString(responseData));
                    if (js != null)
                    {
                        string text = null;
                        for (int i = 0; i < js.Text.Count; i++)
                        {
                            text += "链接：https://www.lanzoux.com/" + js.Text[i].Id + "\n" + "文件名：" + js.Text[i].Name_all + "\n" + "大小：" + js.Text[i].Size + "\n" + "上传时间：" + js.Text[i].Time + "\n";
                        }
                        return text;
                    }
                }
                if (password != null)
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
                    LanzouJsonFilelist js = JsonConvert.DeserializeObject<LanzouJsonFilelist>(Encoding.UTF8.GetString(responseData));
                    if (js != null)
                    {
                        string text = null;
                        for (int i = 0; i < js.Text.Count; i++)
                        {
                            text += "链接：https://www.lanzoux.com/" + js.Text[i].Id + "\n" + "文件名：" + js.Text[i].Name_all + "\n" + "大小：" + js.Text[i].Size + "\n" + "上传时间：" + js.Text[i].Time + "\n";
                        }
                        return text;
                    }
                }
                return "蓝奏云文件夹解析失败...";
            }
        }
        
        internal static async Task<string> 蓝奏云直链解析(string Content , string password = null)
        {
            using (Web Web = new Web())
            {
                string page = await Web.Client.DownloadStringTaskAsync(Content);
                if (password == null)
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
                    LanzouJson js = JsonConvert.DeserializeObject<LanzouJson>(Encoding.UTF8.GetString(responseData));
                    if (js != null)
                    {
                        return Get(js.Dom + "/file/" + js.Url);
                    }
                }
                else if(password != null)
                {
                    string data = new Regex("(?<=data : ')(.*)(?=')").Match(page).Value + password;
                    byte[] postdata = Encoding.UTF8.GetBytes(data);
                    Web.Client.Headers[HttpRequestHeader.Referer] = Content;
                    Web.Client.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
                    byte[] responseData = await Web.Client.UploadDataTaskAsync("https://www.lanzoux.com/ajaxm.php", "POST", postdata);
                    LanzouJson js = JsonConvert.DeserializeObject<LanzouJson>(Encoding.UTF8.GetString(responseData));
                    if (js != null)
                    {
                        return Get(js.Dom + "/file/" + js.Url);
                    }
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
