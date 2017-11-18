using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Net;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using tesseract;

namespace Http
{

    // HTTP 请求类
    class Requests
    {
        // Get 方法
        public Response Get(string Url, string postDataStr, ref CookieContainer cookie, string Referer = "")
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(Url + (postDataStr == "" ? "" : "?") + postDataStr);
            if (cookie.Count == 0)
            {
                request.CookieContainer = new CookieContainer();
                cookie = request.CookieContainer;
            }
            else
            {
                request.CookieContainer = cookie;
            }
            request.AllowAutoRedirect = false;
            request.Method = "GET";
            request.ContentType = "text/html;charset=UTF-8";
            request.Referer = Referer;

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            Response r = new Response(response);
            return r;

        }
        // Post方法
        public Response Post(string Url, string postDataStr, ref CookieContainer cookie)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(Url);
            if (cookie.Count == 0)
            {
                request.CookieContainer = new CookieContainer();
                cookie = request.CookieContainer;
            }
            else
            {
                request.CookieContainer = cookie;
            }

            request.AllowAutoRedirect = false;
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = postDataStr.Length;
            Stream myRequestStream = request.GetRequestStream();
            StreamWriter myStreamWriter = new StreamWriter(myRequestStream, Encoding.GetEncoding("gb2312"));
            myStreamWriter.Write(postDataStr);
            myStreamWriter.Close();

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();

            Response r = new Response(response);
            return r;
        }
        // 获取CookieContainer中所有的Cookies
        public static List<Cookie> GetAllCookies(CookieContainer cc)
        {
            List<Cookie> lstCookies = new List<Cookie>();

            Hashtable table = (Hashtable)cc.GetType().InvokeMember("m_domainTable",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.GetField |
                System.Reflection.BindingFlags.Instance, null, cc, new object[] { });

            foreach (object pathList in table.Values)
            {
                SortedList lstCookieCol = (SortedList)pathList.GetType().InvokeMember("m_list",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.GetField
                    | System.Reflection.BindingFlags.Instance, null, pathList, new object[] { });
                foreach (CookieCollection colCookies in lstCookieCol.Values)
                    foreach (Cookie c in colCookies) lstCookies.Add(c);
            }

            return lstCookies;
        }

        // 保存 Cookies 文件（文本文件方式）
        public static void SaveCookies(CookieContainer cc, string filename = "cookiesTemp")
        {
            StringBuilder sbc = new StringBuilder();
            List<Cookie> cooklist = Requests.GetAllCookies(cc);
            foreach (Cookie cookie in cooklist)
            {
                sbc.AppendFormat("{0};{1};{2};{3};{4};{5}\r\n",
                    cookie.Domain, cookie.Name, cookie.Path, cookie.Port,
                    cookie.Secure.ToString(), cookie.Value);
            }
            FileStream fs = File.Create(@".\Cookies\" + filename + ".txt");
            fs.Close();
            File.WriteAllText(@".\Cookies\" + filename + ".txt", sbc.ToString(), System.Text.Encoding.Default);
        }
        // 读取 Cookies 文件（文本文件方式）
        public static CookieContainer ReadCookies(string filename = "cookiesTemp")
        {
            string[] cookies = File.ReadAllText(@"./Cookies/" + filename + ".txt", System.Text.Encoding.Default).Split("\r\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            CookieContainer cookiecontainer = new CookieContainer();
            foreach (string c in cookies)
            {
                string[] cc = c.Split(";".ToCharArray());

                Cookie ck = new Cookie
                {
                    Discard = false,
                    Domain = cc[0],
                    Expired = false,
                    HttpOnly = true,
                    Name = cc[1],
                    Path = cc[2],
                    Port = cc[3],
                    Secure = bool.Parse(cc[4]),
                    Value = cc[5]
                };
                cookiecontainer.Add(ck);
            }
            return cookiecontainer;
        }

        
        // <静态> 保存Cookies文件（二进制文件方式）
        public static void SaveCookies2(CookieContainer cc, string filename = "cookiesTemp")
        {

            using (Stream stream = File.Create(@".\Cookies\" + filename))
            {
                try
                {
                    BinaryFormatter formatter = new BinaryFormatter();
                    formatter.Serialize(stream, cc);
                }
                catch (Exception e)
                {
                    Console.Out.WriteLine("Problem writing cookies to disk: " + e.GetType());
                }
            }
        }
        // <静态> 读取 Cookies 文件（二进制文件方式）
        public static CookieContainer ReadCookies2(string filename = "cookiesTemp")
        {
            string file = @"./Cookies/" + filename;
            try
            {
                using (Stream stream = File.Open(file, FileMode.Open))
                {
                    BinaryFormatter formatter = new BinaryFormatter();
                    return (CookieContainer)formatter.Deserialize(stream);
                }
            }
            catch (Exception e)
            {
                Console.Out.WriteLine("Problem reading cookies from disk: " + e.GetType());
                return new CookieContainer();
            }

        }
        // <静态> CookieContainer 对象信息预览（测试用）
        public static void CookiesView(CookieContainer cc)
                {
                    StringBuilder sbc = new StringBuilder();
                    List<Cookie> cooklist = Requests.GetAllCookies(cc);
                    foreach (Cookie cookie in cooklist)
                    {
                        sbc.AppendFormat("{0};{1};{2};{3};{4};{5}\r\n",
                            cookie.Domain, cookie.Name, cookie.Path, cookie.Port,
                            cookie.Secure.ToString(), cookie.Value);
                    }
                    Console.WriteLine("-------------------------------------------------");
                    Console.WriteLine(sbc);
                    Console.WriteLine("-------------------------------------------------");
                }
    }

    // Http 返回类
    class Response
    {   
        public HttpWebResponse response = null; // 系统 HttpWebResponse 对象

        // 构造函数
        public Response(HttpWebResponse response)
        {
            this.response = response;
        }
        // 获取文本返回值
        public string getText()
        {
            if (response == null)
            {
                return null;
            }
            Stream myResponseStream = response.GetResponseStream();
            StreamReader myStreamReader = new StreamReader(myResponseStream, Encoding.GetEncoding("utf-8"));
            string retString = myStreamReader.ReadToEnd();
            myStreamReader.Close();
            myResponseStream.Close();
            return retString;
        }
        // 获取图片返回值
        public Bitmap getImg()
        {
            Bitmap img = null;
            if (response != null)
            {
                Stream myResponseStream = response.GetResponseStream();
                img = new Bitmap(myResponseStream);//获取图片
                myResponseStream.Close();
            }
            return img;
        }
        // 获取流返回值
        public StreamReader getStream()
        {
            if (response == null)
            {
                return null;
            }
            Stream myResponseStream = response.GetResponseStream();
            StreamReader myStreamReader = new StreamReader(myResponseStream, Encoding.GetEncoding("utf-8"));
            return myStreamReader;
        }
    }
    
    // 验证码识别类
    class SafeCode
    {
        public enum TesseractEngineMode : int
        {
            /// <summary>
            /// Run Tesseract only - fastest
            /// </summary>
            TESSERACT_ONLY = 0,

            /// <summary>
            /// Run Cube only - better accuracy, but slower
            /// </summary>
            CUBE_ONLY = 1,

            /// <summary>
            /// Run both and combine results - best accuracy
            /// </summary>
            TESSERACT_CUBE_COMBINED = 2,

            /// <summary>
            /// Specify this mode when calling init_*(),
            /// to indicate that any of the above modes
            /// should be automatically inferred from the
            /// variables in the language-specific config,
            /// command-line configs, or if not specified
            /// in any of the above should be set to the
            /// default OEM_TESSERACT_ONLY.
            /// </summary>
            DEFAULT = 3
        }
        public enum TesseractPageSegMode : int
        {
            /// <summary>
            /// Fully automatic page segmentation
            /// </summary>
            PSM_AUTO = 0,

            /// <summary>
            /// Assume a single column of text of variable sizes
            /// </summary>
            PSM_SINGLE_COLUMN = 1,

            /// <summary>
            /// Assume a single uniform block of text (Default)
            /// </summary>
            PSM_SINGLE_BLOCK = 2,

            /// <summary>
            /// Treat the image as a single text line
            /// </summary>
            PSM_SINGLE_LINE = 3,

            /// <summary>
            /// Treat the image as a single word
            /// </summary>
            PSM_SINGLE_WORD = 4,

            /// <summary>
            /// Treat the image as a single character
            /// </summary>
            PSM_SINGLE_CHAR = 5
        }
        private const string m_path = @".\data\"; // 存放训练库的文件夹（同级的data文件夹下）
        private const string m_lang = "eng";      // 识别的语言文件名称

        public string read2dig(Image image)
        {

            TesseractProcessor m_tesseract = new TesseractProcessor();

            ;
            bool succeed = m_tesseract.Init(m_path, m_lang, (int)TesseractEngineMode.DEFAULT);
            if (!succeed)
            {
                return "tesseract初始化失败";
            }
            m_tesseract.SetVariable("tessedit_char_whitelist", "0123456789"); //设置识别变量，当前只能识别数字。

            image.Save("./img/原图.bmp", System.Drawing.Imaging.ImageFormat.Bmp);
            Bitmap bmp = ToGray(image);
            bmp.Save("./img/灰度图.bmp", System.Drawing.Imaging.ImageFormat.Bmp);
            bmp = ConvertTo1Bpp1(bmp, 110);
            bmp.Save("./img/二值图.bmp", System.Drawing.Imaging.ImageFormat.Bmp);
            bmp = Zoom(bmp, 0.25);
            bmp.Save("./img/放大缩小后.bmp", System.Drawing.Imaging.ImageFormat.Bmp);
            m_tesseract.Clear();
            m_tesseract.ClearAdaptiveClassifier();
            return m_tesseract.Apply(bmp);

        }

        private Bitmap ToGray(Image image)
        {
            Bitmap bmp = (Bitmap)image;
            for (int i = 0; i < bmp.Width; i++)
            {
                for (int j = 0; j < bmp.Height; j++)
                {
                    //获取该点的像素的RGB的颜色
                    Color color = bmp.GetPixel(i, j);
                    //利用公式计算灰度值
                    int gray = (int)(color.R * 0.3 + color.G * 0.59 + color.B * 0.11);
                    Color newColor = Color.FromArgb(gray, gray, gray);
                    bmp.SetPixel(i, j, newColor);
                }
            }
            return bmp;
        }

        private Bitmap ConvertTo1Bpp1(Bitmap bmp, int average = 0)
        {
            if (average == 0)
            {
                for (int i = 0; i < bmp.Width; i++)
                {
                    for (int j = 0; j < bmp.Height; j++)
                    {
                        Color color = bmp.GetPixel(i, j);
                        average += color.R;
                    }
                    average = (int)average / (bmp.Width * bmp.Height);
                }
            }

            for (int i = 0; i < bmp.Width; i++)
            {
                for (int j = 0; j < bmp.Height; j++)
                {
                    //获取该点的像素的RGB的颜色
                    Color color = bmp.GetPixel(i, j);
                    int value = 255 - color.B;
                    Color newColor = value > average ? Color.FromArgb(0, 0, 0) : Color.FromArgb(255, 255, 255);
                    bmp.SetPixel(i, j, newColor);
                }
            }
            return bmp;
        }

        private Bitmap Zoom(Bitmap bm, double times)
        {
            int nowHeight = (int)(bm.Height / times);
            int nowWidth = (int)(bm.Width / times);
            Bitmap newbm = new Bitmap(nowWidth, nowHeight);//新建一个放大后大小的图片 
            if (times >= 1 && times <= 1.1)
            {
                newbm = bm;
            }
            else
            {
                Graphics g = Graphics.FromImage(newbm);
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.CompositingQuality = CompositingQuality.HighQuality;
                g.DrawImage(bm, new Rectangle(0, 0, nowWidth, nowHeight), new Rectangle(0, 0, bm.Width, bm.Height), GraphicsUnit.Pixel);
                g.Dispose();
            }
            return newbm;
        }

    }
}