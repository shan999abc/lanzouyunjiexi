using System;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace TEST
{
    internal class Download
    {
        internal static async Task<string> 关键字解析(string link)
        {
            if (link.Contains("mail.qq"))
            {
                return await QQ邮箱直链解析(link);
            }
            else if (link.Contains("lanzou"))
            {
                string domain = "https://" + Regex.Match(link, "(\\w*\\.){2}\\w*").Value;
                string password = Regex.Match(link, "(?<=&pwd=).*").Value;
                if (link.EndsWith("&folder"))
                {
                    return await 蓝奏云文件夹解析(domain, link.Replace($"&pwd={password}", "").Replace("&folder", ""), password.Replace("&folder", ""));
                }
                return await 蓝奏云直链解析(domain, link.Replace($"&pwd={password}", ""), password);
            }
            else if (link.Contains("123pan"))
            {
                string password = Regex.Match(link, "(?<=&pwd=).*").Value;
                string ID = Regex.Replace(link.Replace($"&pwd={password}", ""), ".*/","");
                if (link.EndsWith("&folder"))
                {
                   return await Ottpanfolder(ID.Replace("&folder", ""), password.Replace("&folder", ""));
                }
                return await Ottpan(ID, password);
            }
            return "无法获取正确的链接对象...";
        }

        internal static async Task<string> Ottpan(string Content , string password = "")
        {
            string posturl = $"https://www.123pan.com/b/api/share/get?limit=100&next=1&orderBy=share_id&orderDirection=desc&shareKey={Content}&SharePwd={password}&ParentFileId=0&Page=1";
            dynamic ottpanjson = Json.DeserializeObject(await DownloadString(posturl));
            if ($"{ottpanjson["code"]}" == "0")
            {
                return await Get($"{ottpanjson["data"]["InfoList"][0]["DownloadUrl"]}&filename={ottpanjson["data"]["InfoList"][0]["FileName"]}");
            }
            return $"错误：{ottpanjson["message"]}";
        }
        internal static async Task<string> Ottpanfolder(string Content, string password = "")
        {
            string posturl = $"https://www.123pan.com/b/api/share/get?limit=100&next=1&orderBy=share_id&orderDirection=desc&shareKey={Content}&SharePwd={password}&ParentFileId=0&Page=1";
            dynamic ottpanjson = Json.DeserializeObject(await DownloadString(posturl));
            if ($"{ottpanjson["code"]}" == "0")
            {
                string folderindex = $"{ottpanjson["data"]["InfoList"][0]["FileId"]}";
                string posturlfolder = $"https://www.123pan.com/b/api/share/get?limit=100&next=1&orderBy=share_id&orderDirection=desc&shareKey={Content}&SharePwd={password}&ParentFileId={folderindex}&Page=1";
                dynamic ottpanjsonfolder = Json.DeserializeObject(await DownloadString(posturlfolder));
                if ($"{ottpanjsonfolder["code"]}" == "0")
                {
                    string files = null;
                    for (int i = 0; i < Convert.ToInt32($"{ottpanjsonfolder["data"]["InfoList"].Length}"); i++)
                    {
                        files += $"文件名：{ottpanjsonfolder["data"]["InfoList"][i]["FileName"]}\n文件直链：{await Get($"{ottpanjsonfolder["data"]["InfoList"][i]["DownloadUrl"]}&filename={ottpanjsonfolder["data"]["InfoList"][i]["FileName"]}")}\n\n----------------------------------------------------\n\n";
                    }
                    return files;
                }
                return $"错误：{ottpanjsonfolder["message"]}";
            }
            return $"错误：{ottpanjson["message"]}";
        }
        internal static async Task<string> 蓝奏云直链解析(string domain ,string Content , string password = "")
        {
            string page = await DownloadString(Content);
            if (Msg(page, true) != "True")
            {
                if (password == "")
                {
                    string data = $"action=downprocess&sign={RegexAll(await DownloadString(domain + RegexAll(page, "/fn[^\"]+")), "\\w*_c")}&ves=1";
                    string responseData = await UploadData(Content, $"{domain}/ajaxm.php", Encoding.UTF8.GetBytes(data));
                    if (responseData != "")
                    {
                        return await JsonDeserialize(responseData);
                    }
                }
                else if (password != "")
                {
                    string data = $"action=downprocess&sign={RegexAll(page, "\\w*_c")}&ves=1&p={password}";
                    string responseData = await UploadData(Content, $"{domain}/ajaxm.php", Encoding.UTF8.GetBytes(data));
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
        internal static async Task<string> 蓝奏云文件夹解析(string domain, string Content, string password = "")
        {
            string page = await DownloadString(Content);
            if (password == "")
            {
                string t = Regex.Match(page, "(?<='t':)(.*)(?=,)").Value;
                string k = Regex.Match(page, "(?<='k':)(.*)(?=,)").Value;
                string lx = Regex.Match(page, "(?<=lx':)(.*)(?=,)").Value;
                string fid = Regex.Match(page, "(?<=fid':)(.*)(?=,)").Value;
                string uid = Regex.Match(page, "(?<=uid':')(.*)(?=')").Value;
                string t1 = Regex.Match(page, $"(?<={t} = ')(.*)(?=')").Value;
                string k1 = Regex.Match(page, $"(?<={k} = ')(.*)(?=')").Value;
                string data = $"lx={lx}&fid={fid}&uid={uid}&pg=1&rep=0&t={t1}&k={k1}&up=1&vip=0&webfoldersign=";
                string responseData = await UploadData(Content, $"{domain}/filemoreajax.php", Encoding.UTF8.GetBytes(data));
                if (responseData != "")
                {
                    return JsonDeserializexFolder(domain, responseData);
                }
            }
            else if (password != "")
            {
                string t = Regex.Match(page, "(?<='t':)(.*)(?=,)").Value;
                string k = Regex.Match(page, "(?<='k':)(.*)(?=,)").Value;
                string lx = Regex.Match(page, "(?<=lx':)(.*)(?=,)").Value;
                string fid = Regex.Match(page, "(?<=fid':)(.*)(?=,)").Value;
                string uid = Regex.Match(page, "(?<=uid':')(.*)(?=')").Value;
                string t1 = Regex.Match(page, $"(?<={t} = ')(.*)(?=')").Value;
                string k1 = Regex.Match(page, $"(?<={k} = ')(.*)(?=')").Value;
                string data = $"lx={lx}&fid={fid}&uid={uid}&pg=1&rep=0&t={t1}&k={k1}&up=1&ls=1&pwd={password}";
                string responseData = await UploadData(Content, $"{domain}/filemoreajax.php", Encoding.UTF8.GetBytes(data));
                if (responseData != "")
                {
                    return JsonDeserializexFolder(domain, responseData);
                }
            }
            return "蓝奏云文件夹解析失败...";
        }
        internal static async Task<string> QQ邮箱直链解析(string Content)
        {
            foreach (Match link in Regex.Matches(await DownloadString(Content), "http[^\"]+"))
            {
                if (link.Value.Contains("download.ftn.qq.com"))
                {
                    return link.Value;
                }
            }
            return "QQ邮箱直链解析失败...";
        }

        private static string Msg(string msg, bool m = false)
        {
            Match match = Regex.Match(msg, "(?<=<div class=\"off\"><div class=\"off0\"><div class=\"off1\"></div></div>).*(?=</div>)");
            if (m)
            {
                return match.Success.ToString();
            }
            return match.Value;
        }

        private static string RegexAll(string page,string pattern)
        {
            string x = "";
            foreach (Match m in Regex.Matches(page, pattern))
            {
                if (m.Length > 70)
                {
                    x = m.Value;
                }
            }
            return x;
        }

        internal static async Task<string> DownloadString(string url)
        {
            using (Web Web = new Web())
            return await Web.Client.DownloadStringTaskAsync(url);
        }

        private static async Task<string> UploadData(string Referer, string address, byte[] postdata)
        {
            using (Web Web = new Web(Referer, is_upload: true))
            return Encoding.UTF8.GetString(await Web.Client.UploadDataTaskAsync(address, "POST", postdata));
        }

        private static async Task<string> Get(string link)
        {
            HttpWebRequest Request = WebRequest.Create(link) as HttpWebRequest;
            Request.Headers.Set("accept-language", "zh-CN,zh;q=0.9");
            Request.AllowAutoRedirect = false;
            WebResponse Response = await Request.GetResponseAsync();
            string RedirectLink = Response.Headers.Get("Location");
            Response.Close();
            Request.Abort();
            return RedirectLink;
        }

        private static async Task<string> JsonDeserialize(string responseData)
        {
            dynamic lanzoujson = Json.DeserializeObject(responseData);
            if ($"{lanzoujson["zt"]}" == "1")
            {
                return await Get($"{lanzoujson["dom"]}/file/{lanzoujson["url"]}");
            }
            return $"错误：{lanzoujson["inf"]}";
        }
        private static string JsonDeserializexFolder(string domain, string responseData)
        {
            dynamic lanzouJsonFolder = Json.DeserializeObject(responseData);
            if ($"{lanzouJsonFolder["zt"]}" == "1")
            {
                string files = null;
                for (int i = 0; i < Convert.ToInt32($"{lanzouJsonFolder["text"].Length}"); i++)
                {
                    files += $"文件名：{lanzouJsonFolder["text"][i]["name_all"]}\n大小：{lanzouJsonFolder["text"][i]["size"]}\n上传时间：{lanzouJsonFolder["text"][i]["time"]}\n链接：{domain}/{Regex.Match(lanzouJsonFolder["text"][i]["id"], "\\w*")}\n\n----------------------------------------------------\n\n";
                }
                return files;
            }
            return $"错误：{lanzouJsonFolder["info"]}";
        }

    }
    internal class Json
    {
        static readonly JavaScriptSerializer JavaScriptSerializer = new JavaScriptSerializer();
        internal static object DeserializeObject(string JsonText)
        {
            return JavaScriptSerializer.DeserializeObject(JsonText);
        }
    }
    public class Web : IDisposable
    {
        public WebClient Client { get; private set; }
        public Web(string Referer = "", bool is_upload = false)
        {
            Client = new WebClient
            {
                Proxy = null,
                Encoding = Encoding.UTF8,
            };
            Client.Headers[HttpRequestHeader.UserAgent] = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/99.9.9999.99 Safari/537.36 Edg/99.9.9999.99";
            Client.Headers[HttpRequestHeader.AcceptLanguage] = "zh-CN,zh;q=0.9";
            if (is_upload)
            {
                Client.Headers[HttpRequestHeader.Referer] = Referer;
                Client.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
            }
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