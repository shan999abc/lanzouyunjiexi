using System;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Sht
{
    internal class 云盘解析
    {
        /// <summary>
        /// 根据传入的链接调用对应的方法
        /// </summary>
        /// <param name="link"></param>
        /// <returns></returns>
        internal static async Task<string> 关键字解析(string link)
        {
            string password = Regex.Match(link, "(?<=&pwd=).*").Value;

            if (link.Contains("mail.qq")) { return await QQ邮箱直链解析(link); }
            else if (link.Contains("lanzou"))
            {
                string domain = "https://" + Regex.Match(link, "(\\w*\\.){2}\\w*").Value;
                if (link.EndsWith("&folder")) { return await 蓝奏云文件夹解析(domain, link.Replace($"&pwd={password}", "").Replace("&folder", ""), password.Replace("&folder", "")); }
                return await 蓝奏云直链解析(domain, link.Replace($"&pwd={password}", ""), password);
            }
            else if (link.Contains("123pan"))
            {
                string ID = Regex.Replace(link.Replace($"&pwd={password}", ""), ".*/", "");
                if (link.EndsWith("&folder")) { return await Ottpanfolder(ID.Replace("&folder", ""), password.Replace("&folder", "")); }
                return await Ottpan(ID, password);
            }
            else if (link.StartsWith("http")) { return link; }
            return "无法获取正确的链接对象...";
        }
        /// <summary>
        /// 根据传入的链接获取重定向后的链接
        /// </summary>
        /// <param name="link">需要被重定向的链接</param>
        /// <returns></returns>
        internal static async Task<string> Get(string link)
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
        /// <summary>
        /// 123云盘直链解析
        /// </summary>
        /// <param name="Content">123云盘分享链接</param>
        /// <param name="password">123云盘分享密码</param>
        /// <returns></returns>
        internal static async Task<string> Ottpan(string Content, string password = "")
        {
            string result;
            string posturl = $"https://www.123pan.com/b/api/share/get?limit=100&next=1&orderBy=share_id&orderDirection=desc&shareKey={Content}&SharePwd={password}&ParentFileId=0&Page=1";
            dynamic ottpanjson = Json.DeserializeObject(await Web.DownloadString(posturl));
            if ($"{ottpanjson["code"]}" == "0")
            {
                string postjson = $"{{\"ShareKey\":\"{Content}\",\"FileID\":{ottpanjson["data"]["InfoList"][0]["FileId"]},\"S3keyFlag\":\"{ottpanjson["data"]["InfoList"][0]["S3KeyFlag"]}\",\"Size\":{ottpanjson["data"]["InfoList"][0]["Size"]},\"Etag\":\"{ottpanjson["data"]["InfoList"][0]["Etag"]}\"}}";
                result = await Web.UploadData("", "https://www.123pan.com/a/api/share/download/info", Encoding.UTF8.GetBytes(postjson));
                dynamic ottpanjson2 = Json.DeserializeObject(result);
                if ($"{ottpanjson2["code"]}" == "0") { result = await Get(Encoding.UTF8.GetString(Convert.FromBase64String(Regex.Match($"{ottpanjson2["data"]["DownloadURL"]}", "(?<=params=).+").Value))); }
                else { result = $"错误：{ottpanjson2["message"]}"; }
            }
            else { result = $"错误：{ottpanjson["message"]}"; }
            return result;
        }
        /// <summary>
        /// 123云盘文件夹解析
        /// </summary>
        /// <param name="Content">123云盘分享链接</param>
        /// <param name="password">123云盘分享密码</param>
        /// <returns></returns>
        internal static async Task<string> Ottpanfolder(string Content, string password = "")
        {
            string result = "123云盘文件夹解析失败...";
            string posturl = $"https://www.123pan.com/b/api/share/get?limit=100&next=1&orderBy=share_id&orderDirection=desc&shareKey={Content}&SharePwd={password}&ParentFileId=0&Page=1";
            dynamic ottpanjson = Json.DeserializeObject(await Web.DownloadString(posturl));
            if ($"{ottpanjson["code"]}" == "0")
            {
                string folderindex = $"{ottpanjson["data"]["InfoList"][0]["FileId"]}";
                string posturlfolder = $"https://www.123pan.com/b/api/share/get?limit=100&next=1&orderBy=share_id&orderDirection=desc&shareKey={Content}&SharePwd={password}&ParentFileId={folderindex}&Page=1";
                dynamic ottpanjsonfolder = Json.DeserializeObject(await Web.DownloadString(posturlfolder));
                if ($"{ottpanjsonfolder["code"]}" == "0")
                {
                    StringBuilder files = new StringBuilder().Clear();
                    for (int i = 0; i < Convert.ToInt32($"{ottpanjsonfolder["data"]["InfoList"].Length}"); i++)
                    {
                        if ($"{ottpanjsonfolder["data"]["InfoList"][i]["Status"]}" != "0")
                        {
                            string postjson = $"{{\"ShareKey\":\"{Content}\",\"FileID\":{ottpanjsonfolder["data"]["InfoList"][i]["FileId"]},\"S3keyFlag\":\"{ottpanjsonfolder["data"]["InfoList"][i]["S3KeyFlag"]}\",\"Size\":{ottpanjsonfolder["data"]["InfoList"][i]["Size"]},\"Etag\":\"{ottpanjsonfolder["data"]["InfoList"][i]["Etag"]}\"}}";
                            result = await Web.UploadData("", "https://www.123pan.com/a/api/share/download/info", Encoding.UTF8.GetBytes(postjson));
                            dynamic ottpanjson2 = Json.DeserializeObject(result);
                            if ($"{ottpanjson2["code"]}" == "0") { files.Append($"文件名：{ottpanjsonfolder["data"]["InfoList"][i]["FileName"]}\n文件直链：\n{await Get(Encoding.UTF8.GetString(Convert.FromBase64String(Regex.Match($"{ottpanjson2["data"]["DownloadURL"]}", "(?<=params=).+").Value)))}\n\n----------------------------------------------------\n\n"); }
                            else { result = $"错误：{ottpanjson2["message"]}"; }
                        }
                    }
                    if (!string.IsNullOrEmpty(files.ToString())) { result = files.ToString(); }
                }
                else { result = $"错误：{ottpanjsonfolder["message"]}"; }
            }
            else { result = $"错误：{ottpanjson["message"]}"; }
            return result;
        }
        /// <summary>
        /// 传入蓝奏云页面匹配错误信息
        /// </summary>
        /// <param name="msg">蓝奏云的页面</param>
        /// <param name="m"></param>
        /// <returns></returns>
        private static string Msg(string msg, bool m = false)
        {
            Match match = Regex.Match(msg, "(?<=<div class=\"off\"><div class=\"off0\"><div class=\"off1\"></div></div>).*(?=</div>)");
            if (m) { return match.Success.ToString(); }
            return match.Value;
        }
        /// <summary>
        /// 使用正则匹配长度大于70的指定文本
        /// </summary>
        /// <param name="page"></param>
        /// <param name="pattern"></param>
        /// <returns></returns>
        private static string RegexAll(string page, string pattern)
        {
            foreach (Match m in Regex.Matches(page, pattern)) { if (m.Length > 70) { return m.Value; } }
            return "使用正则匹配特定长度文本失败...";
        }
        /// <summary>
        /// 蓝奏云直链解析
        /// </summary>
        /// <param name="domain"></param>
        /// <param name="Content">蓝奏云分享链接</param>
        /// <param name="password">蓝奏云分享密码</param>
        /// <returns></returns>
        private static async Task<string> 蓝奏云直链解析(string domain, string Content, string password = "")
        {
            string result = "蓝奏云直链解析失败...";
            string page = await Web.DownloadString(Content);
            if (Msg(page, true) != "True")
            {
                string postdata;
                if (password == "") { postdata = $"action=downprocess&sign={RegexAll(await Web.DownloadString(domain + RegexAll(page, "/fn[^\"]+")), "\\w*_c")}&ves=1"; }
                else { postdata = $"action=downprocess&sign={RegexAll(page, "\\w*_c")}&ves=1&p={password}"; }
                if (postdata != "")
                {
                    result = await Web.UploadData(Content, $"{domain}/ajaxm.php", Encoding.UTF8.GetBytes(postdata));
                    dynamic lanzoujson = Json.DeserializeObject(result);
                    if ($"{lanzoujson["zt"]}" == "1") { result = await Get($"{lanzoujson["dom"]}/file/{lanzoujson["url"]}"); }
                    else { result = $"错误：{lanzoujson["inf"]}"; }
                }
            }
            else { result = $"错误：{Msg(page)}"; }
            return result;
        }
        /// <summary>
        /// 蓝奏云文件夹解析
        /// </summary>
        /// <param name="domain"></param>
        /// <param name="Content">蓝奏云文件夹分享链接</param>
        /// <param name="password">蓝奏云文件夹分享密码</param>
        /// <returns></returns>
        private static async Task<string> 蓝奏云文件夹解析(string domain, string Content, string password = "")
        {
            string result = "蓝奏云文件夹解析失败...";
            string page = await Web.DownloadString(Content);
            string postdata;
            string[] values = { "", "", "", "", "" };
            string[] patterns = { "(?<='t':)(.*)(?=,)", "(?<='k':)(.*)(?=,)", "(?<=lx':)(.*)(?=,)", "(?<=fid':)(.*)(?=,)", "(?<=uid':')(.*)(?=')" };
            for (int i = 0; i < patterns.Length; i++) { values[i] = Regex.Match(page, patterns[i]).Value; }
            if (password == "") { postdata = $"lx={values[2]}&fid={values[3]}&uid={values[4]}&pg=1&rep=0&t={Regex.Match(page, $"(?<={values[0]} = ')(.*)(?=')").Value}&k={Regex.Match(page, $"(?<={values[1]} = ')(.*)(?=')").Value}&up=1&vip=0&webfoldersign="; }
            else { postdata = $"lx={values[2]}&fid={values[3]}&uid={values[4]}&pg=1&rep=0&t={Regex.Match(page, $"(?<={values[0]} = ')(.*)(?=')").Value}&k={Regex.Match(page, $"(?<={values[1]} = ')(.*)(?=')").Value}&up=1&ls=1&pwd={password}"; }
            if (postdata != "")
            {
                result = await Web.UploadData(Content, $"{domain}/filemoreajax.php", Encoding.UTF8.GetBytes(postdata));
                dynamic lanzouJsonFolder = Json.DeserializeObject(result);
                if ($"{lanzouJsonFolder["zt"]}" == "1")
                {
                    StringBuilder files = new StringBuilder().Clear();
                    for (int i = 0; i < Convert.ToInt32($"{lanzouJsonFolder["text"].Length}"); i++) { files.Append($"文件名：{lanzouJsonFolder["text"][i]["name_all"]}\n大小：{lanzouJsonFolder["text"][i]["size"]}\n上传时间：{lanzouJsonFolder["text"][i]["time"]}\n链接：{domain}/{Regex.Match(lanzouJsonFolder["text"][i]["id"], "\\w*")}\n\n----------------------------------------------------\n\n"); }
                    result = files.ToString();
                }
                else { result = $"错误：{lanzouJsonFolder["info"]}"; }
            }
            return result;
        }
        /// <summary>
        /// QQ邮箱文件中转站直链解析
        /// </summary>
        /// <param name="Content">分享链接</param>
        /// <returns></returns>
        internal static async Task<string> QQ邮箱直链解析(string Content)
        {
            foreach (Match link in Regex.Matches(await Web.DownloadString(Content), "http[^\"]+")) { if (link.Value.Contains("download.ftn.qq.com")) { return link.Value; } }
            return "QQ邮箱直链解析失败...";
        }
    }
}