using System;
using System.IO;
using System.Text;
using System.Web.Script.Serialization;
using MSScriptControl;

//-------------------------------------------------

/*                  通用函数类                    */

//-------------------------------------------------
namespace Common
{
    class Comm
    {
        // 读取文本文件
        public static string TxTRead(string filepath)
        {
            StreamReader sr = new StreamReader(filepath, Encoding.Default);
            string allLine = "";
            String line;
            while ((line = sr.ReadLine()) != null)
            {
                allLine = allLine + line.ToString() + "\n";
            }
            return allLine;
        }
        // 读取Json字符串（返回字符串字典）
        public static dynamic JsonRead(string jsonStr)
        {
            JavaScriptSerializer js = new JavaScriptSerializer();
            dynamic modelDy = js.Deserialize<dynamic>(jsonStr); //反序列化
            return modelDy;
        }
        // 读取网页文本（测试用，返回流）
        public static StreamReader xmlRead(string path)
        {
            StreamReader reader = new StreamReader(path, Encoding.GetEncoding("utf-8"));
            return reader;
        }
        // 执行Javascript
        public static string ExecuteScript(string sExpression, string sCode)
        {
            ScriptControl scriptControl = new ScriptControl();
            scriptControl.UseSafeSubset = true;
            scriptControl.Language = "JScript";
            scriptControl.AddCode(sCode);
            try
            {
                string str = scriptControl.Eval(sExpression).ToString();
                return str;
            }
            catch (Exception ex)
            {
                string str = ex.Message;
            }
            return null;
        }


    }

}