using System;
using System.IO;
using System.Net;
using System.Text;
using System.Web;

namespace Jad_Bot
{
    class Pastebin
    {
        public static string UploadText(string text, string user)
        {
            try
            {
                String url = "http://jaddie.pastebin.com/pastebin.php";
                HttpWebRequest hwr = (HttpWebRequest)HttpWebRequest.Create(url);
                hwr.Method = "GET";
                hwr.KeepAlive = true;
                string stuff = "parent_pid=&format=c#&code2=" + HttpUtility.HtmlEncode(text) + "&poster=" + user + "&paste=Send&expiry=d&email=";
                hwr.ContentType = "application/x-www-form-urlencoded";
                hwr.Method = "POST";

                hwr.ImpersonationLevel = System.Security.Principal.TokenImpersonationLevel.Impersonation;
                hwr.UserAgent = "Mozilla/5.0 (Windows; U; Windows NT 5.1; en-US) AppleWebKit/525.13 (KHTML, like Gecko) Chrome/0.A.B.C Safari/525.13";
                byte[] bytes = Encoding.ASCII.GetBytes(stuff);
                Stream os = null;
                hwr.ContentLength = bytes.Length;   //Count bytes to send
                os = hwr.GetRequestStream();
                os.Write(bytes, 0, bytes.Length);         //Send it
                os.Close();
                return hwr.GetResponse().ResponseUri.AbsoluteUri;
            }
            catch(Exception e)
            {
                return ("An error occured in the pastebin poster, the following error: " + e.Message);
            }
        }
    }
}
