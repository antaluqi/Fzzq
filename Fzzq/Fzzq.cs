using System;
using System.Net;
using Http;
using System.IO;
using HtmlAgilityPack;
using System.Text.RegularExpressions;
using System.Drawing;
using System.Web;
using Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace Fzzq
{
    class FzzqAPI
    {
        Requests r = new Requests();
        CookieContainer cookies = new CookieContainer();
        public string username;
        private string password;

        public FzzqAPI(string username, string password)
        {
            this.username = username;
            this.password = password;
        }

        public bool Login(ref string reStr)
        {
            string tradeUrl = @"https://trade.foundersc.com/";
            Response resp = r.Get(tradeUrl, "", ref cookies);

            StreamReader streamXML = resp.getStream();
            HtmlDocument doc = new HtmlDocument();
            doc.Load(streamXML);
            //-----------------------------------------------------------------------------------
            HtmlNode node = doc.DocumentNode.SelectSingleNode(@".//input[@id=""__VIEWSTATE""]");
            string VIEWSTATE = node.Attributes["value"].Value;
            //-----------------------------------------------------------------------------------
            node = doc.DocumentNode.SelectSingleNode(@".//input[@id=""__EVENTVALIDATION""]");
            string EVENTVALIDATION = node.Attributes["value"].Value;
            //-----------------------------------------------------------------------------------
            node = doc.DocumentNode.SelectSingleNode(@".//select[@id=""Tcipddl""]/./option");
            string Tcipddl = node.Attributes["value"].Value;
            //-----------------------------------------------------------------------------------
            node = doc.DocumentNode.SelectSingleNode(@".//script[@language]");
            string jsXML = node.InnerText;
            MatchCollection collec = Regex.Matches(jsXML, @"(?<=var key = new RSAKeyPair\(""010001"", """", "")(.*?)(?=""\))");
            string publicKeyN = collec[0].Value.Trim();
            //-----------------------------------------------------------------------------------
            string publicKeyE = "010001";
            //-----------------------------------------------------------------------------------
            string jsFile = File.ReadAllText(@"./javascript/RSA.js");
            string fun = string.Format(@"cmdEncrypt(""{0}"",""{1}"",""{2}"",""{3}"")", publicKeyE, publicKeyN, username, password);
            string posx = Comm.ExecuteScript(fun, jsFile);
            //-----------------------------------------------------------------------------------
            int reTry = 5;
            string sc = "";
            while (reTry > 0)
            {
                Random rd = new Random();
                double nextRandom = rd.NextDouble();
                string safeCodeURL = @"https://trade.foundersc.com//usercenter/checkcode.aspx?" + nextRandom.ToString();
                resp = r.Get(safeCodeURL, "", ref cookies);
                Bitmap img = resp.getImg();
                SafeCode SCRead = new SafeCode();
                sc = SCRead.read2dig(img).Trim();
                if (sc.Length == 5)
                {
                    break;
                }
                reTry--;
                Console.WriteLine("重新识别验证码");
            }

            if (reTry == 0)
            {
                reStr = "验证码错误";
                return false;
            }
            //-----------------------------------------------------------------------------------
            // Console.WriteLine("VIEWSTATE=：  {0}\n\nEVENTVALIDATION=：  {1}\n\nTcipddl=：  {2}\n\nPublicKeyN=：  {3}\n\nPublicKeyE=：  {4}\n\n以上计算得到Posx=： {5}\n\nSafeCode=：  {6}", VIEWSTATE, EVENTVALIDATION, Tcipddl, publicKeyN, publicKeyE, posx,sc);
            //-----------------------------------------------------------------------------------
            string postdic = "__VIEWSTATE=" + HttpUtility.UrlEncode(VIEWSTATE) +
                     "&Text1=" + username +
                     "&hidtxt=" + "" +
                     "&hidkjtxt=" + '1' +
                     "&pwdtxt=" + "" +
                     "&isActive=" + "1" +
                     "&Text2=" + "" +
                     "&HiddenField1=" + "" +
                     "&aqfsddl=" + "1" +
                     "&Text3=" + sc +
                     "&otptxt=" + "" +
                     "&Scddl=" + "9" +
                     "&Yybddl=" + "0001" +
                     "&Tcipddl=" + Tcipddl +
                     "&posx=" + posx +
                     "&__EVENTVALIDATION=" + HttpUtility.UrlEncode(EVENTVALIDATION) +
                     "&hqjybtn.x=" + "72" +
                     "&hqjybtn.y=" + "7";

            // Console.WriteLine(postdic);
            //-----------------------------------------------------------------------------------
            string logURL = @"https://trade.foundersc.com/Fzwt_login.aspx";
            string Referer = @"http://www.foundersc.com/wzweb/entrust/login.action";
            resp = r.Post(logURL, postdic, ref cookies, Referer);
            string Location = resp.response.GetResponseHeader("Location");
            if (Location == "/FzIndex/index.aspx")
            {
                Requests.SaveCookies2(cookies, username);//保存成功登陆的Cookies文件
                reStr = "登陆成功";

                return true;
            }
            else
            {
                reStr = "登陆失败";
                return false;
            }




        }

        public bool LoginFromCookie(ref string reStr)
        {
            // 读取Cookies文件（.符号为上一级）
            string cookieFilePath = @".\Cookies\" + username;
            // 如Cookies文件存在，则载入，反之则提示错误
            if (File.Exists(cookieFilePath))
            {
                cookies = Requests.ReadCookies2(username);
                reStr = "从Cookies登陆成功";

                //string url = @"https://trade.foundersc.com/FzIndex/index.aspx";
                //Response resp = r.Get(url, "", ref cookies);
                //Console.WriteLine(resp.getText());
                return true;
            }
            else
            {
                reStr = "从Cookies登陆失败，没有此用户的Cookies文件";
                return false;
            }
        }

        public void CustomAccountInfo()
        {
            string url = @"https://trade.foundersc.com/content/Zjgf.aspx";
            Response resp = r.Get(url, "", ref cookies);
            //Console.WriteLine(resp.getText());
            StreamReader streamXML = resp.getStream();
            HtmlDocument doc = new HtmlDocument();
            doc.Load(streamXML);
            HtmlNodeCollection collection = doc.DocumentNode.SelectNodes(@".//table[@class=""mtb""]");
            HtmlNode nodeTable = collection[0];
            //----------------------------------------------------------------------------
            HtmlNode node = nodeTable.SelectSingleNode(".//tr/td/table/tr/td[2]");
            string str = node.InnerText;
            MatchCollection collec = Regex.Matches(str, @"(?<=nbsp;)([\w,\s,\S]+)");
            string rmb = collec[0].Value.Trim();
            Console.WriteLine("人民币余额:" + rmb);
            //----------------------------------------------------------------------------
            node = nodeTable.SelectSingleNode(".//tr/td/table/tr/td[4]");
            str = node.InnerText;
            collec = Regex.Matches(str, @"(?<=nbsp;)([\w,\s,\S]+)");
            string balance = collec[0].Value.Trim();
            Console.WriteLine("可用资金：" + balance);
            //----------------------------------------------------------------------------
            node = nodeTable.SelectSingleNode(".//tr/td/table/tr/td[6]");
            str = node.InnerText;
            collec = Regex.Matches(str, @"(?<=nbsp;)([\w,\s,\S]+)");
            string market_value = collec[0].Value.Trim();
            Console.WriteLine("当前市值：" + market_value);
            //----------------------------------------------------------------------------
            node = nodeTable.SelectSingleNode(".//tr/td/table/tr/td[8]");
            str = node.InnerText;
            collec = Regex.Matches(str, @"(?<=nbsp;)([\w,\s,\S]+)");
            string assets = collec[0].Value.Trim();
            Console.WriteLine("资产：" + assets);
            //----------------------------------------------------------------------------
            node = nodeTable.SelectSingleNode(".//tr/td/table/tr/td[10]");
            str = node.InnerText;
            collec = Regex.Matches(str, @"(?<=nbsp;)([\w,\s,\S]+)");
            string percentage = collec[0].Value.Trim();
            Console.WriteLine("浮动盈亏：" + percentage);

        }

        public void CustomStockInfo()
        {
            string url = @"https://trade.foundersc.com/content/Zjgf.aspx";
            Response resp = r.Get(url, "", ref cookies);
            //Console.WriteLine(resp.getText());
            StreamReader streamXML = resp.getStream();
            HtmlDocument doc = new HtmlDocument();
            doc.Load(streamXML);
            HtmlNodeCollection collection = doc.DocumentNode.SelectNodes(@".//table[@class=""mtb""]");
            HtmlNode nodeTable = collection[2];
            //----------------------------------------------------------------------------
            HtmlNodeCollection trCollection = nodeTable.SelectNodes(@".//tr");
            foreach (HtmlNode trNode in trCollection)
            {
                //----------------------------------------------------------------------------
                HtmlNode tdNode = trNode.SelectSingleNode(@".//td[2]");
                string Name = tdNode.InnerText.Trim();
                Console.WriteLine("证券名称:" + Name);
                //----------------------------------------------------------------------------
                tdNode = trNode.SelectSingleNode(@".//td[3]");
                string Amomunt = tdNode.InnerText.Trim();
                Console.WriteLine("证券数量:" + Amomunt);
                //----------------------------------------------------------------------------
                tdNode = trNode.SelectSingleNode(@".//td[4]");
                string CouldBeSold = tdNode.InnerText.Trim();
                Console.WriteLine("可卖数量:" + CouldBeSold);
                //----------------------------------------------------------------------------
                tdNode = trNode.SelectSingleNode(@".//td[5]");
                HtmlNode valueNode = tdNode.SelectSingleNode(@"input");
                string CostPrice = valueNode.Attributes["value"].Value.Trim();
                Console.WriteLine("成本价:" + CostPrice);
                //----------------------------------------------------------------------------
                tdNode = trNode.SelectSingleNode(@".//td[6]");
                string Percentage = tdNode.InnerText.Trim();
                Console.WriteLine("浮动盈亏:" + Percentage);
                //----------------------------------------------------------------------------
                tdNode = trNode.SelectSingleNode(@".//td[7]");
                string MarketValue = tdNode.InnerText.Trim();
                Console.WriteLine("最新市值:" + MarketValue);
                //----------------------------------------------------------------------------
                tdNode = trNode.SelectSingleNode(@".//td[8]");
                string LastPrice = tdNode.InnerText.Trim();
                Console.WriteLine("当前价:" + LastPrice);
                //----------------------------------------------------------------------------
                tdNode = trNode.SelectSingleNode(@".//td[9]");
                string BuyToday = tdNode.InnerText.Trim();
                BuyToday = BuyToday == null ? BuyToday : "0";
                Console.WriteLine("今买数量:" + BuyToday);
                //----------------------------------------------------------------------------
                tdNode = trNode.SelectSingleNode(@".//td[10]");
                string Code = tdNode.InnerText.Trim();
                Console.WriteLine("股票代码:" + Code);
                //----------------------------------------------------------------------------
                tdNode = trNode.SelectSingleNode(@".//td[11]");
                string CustomCode = tdNode.InnerText.Trim();
                Console.WriteLine("股东代码:" + CustomCode);
                //----------------------------------------------------------------------------

                Console.WriteLine("-----------------------------------------");
            }

        }

        public void DayOrder()
        {
            string url = @"https://trade.foundersc.com/WTCX/Drwtcx.aspx";
            Response resp = r.Get(url, "", ref cookies);
            //Console.WriteLine(resp.getText());
            StreamReader streamXML = resp.getStream();
            HtmlDocument doc = new HtmlDocument();
            doc.Load(streamXML);
            HtmlNodeCollection collection = doc.DocumentNode.SelectNodes(@".//table[@class=""mtb""]");
            HtmlNode nodeTable = collection[1];
            //----------------------------------------------------------------------------
            HtmlNodeCollection trCollection = nodeTable.SelectNodes(@".//tr");
            if (trCollection == null)
            {
                Console.WriteLine("当日委托为空");
                return;
            }
            foreach (HtmlNode trNode in trCollection)
            {
                //----------------------------------------------------------------------------
                HtmlNode tdNode = trNode.SelectSingleNode(@".//td[1]");
                string Name = tdNode.InnerText.Trim();
                Console.WriteLine("证券名称:" + Name);
                //----------------------------------------------------------------------------
                tdNode = trNode.SelectSingleNode(@".//td[2]");
                string TradeType = tdNode.InnerText.Trim();
                Console.WriteLine("买卖标志:" + TradeType);
                //----------------------------------------------------------------------------
                tdNode = trNode.SelectSingleNode(@".//td[3]");
                string OrderPrice = tdNode.InnerText.Trim();
                Console.WriteLine("委托价格:" + OrderPrice);
                //----------------------------------------------------------------------------
                tdNode = trNode.SelectSingleNode(@".//td[4]");
                string OrderNum = tdNode.InnerText.Trim();
                Console.WriteLine("委托数量:" + OrderNum);
                //----------------------------------------------------------------------------
                tdNode = trNode.SelectSingleNode(@".//td[5]");
                string DealPrice = tdNode.InnerText.Trim();
                Console.WriteLine("成交价格:" + DealPrice);
                //----------------------------------------------------------------------------
                tdNode = trNode.SelectSingleNode(@".//td[6]");
                string DealNum = tdNode.InnerText.Trim();
                Console.WriteLine("成交数量:" + DealNum);
                //----------------------------------------------------------------------------
                tdNode = trNode.SelectSingleNode(@".//td[7]");
                string Status = tdNode.InnerText.Trim();
                Console.WriteLine("状态说明:" + Status);
                //----------------------------------------------------------------------------
                tdNode = trNode.SelectSingleNode(@".//td[8]");
                string OrderTime = tdNode.InnerText.Trim();
                Console.WriteLine("委托时间:" + OrderTime);
                //----------------------------------------------------------------------------
                tdNode = trNode.SelectSingleNode(@".//td[9]");
                string OrderNo = tdNode.InnerText.Trim();
                Console.WriteLine("委托编号:" + OrderNo);
                //----------------------------------------------------------------------------
                tdNode = trNode.SelectSingleNode(@".//td[10]");
                string Code = tdNode.InnerText.Trim();
                Console.WriteLine("证券代码:" + Code);
                //----------------------------------------------------------------------------
                tdNode = trNode.SelectSingleNode(@".//td[11]");
                string CustomCode = tdNode.InnerText.Trim();
                Console.WriteLine("股东代码:" + CustomCode);
                //----------------------------------------------------------------------------
                tdNode = trNode.SelectSingleNode(@".//td[12]");
                string OrderType = tdNode.InnerText.Trim();
                Console.WriteLine("报价方式:" + OrderType);
            }


        }

        public void DayDeal()
        {
            string url = @"https://trade.foundersc.com/WTCX/Drcjcx.aspx";
            Response resp = r.Get(url, "", ref cookies);
            //Console.WriteLine(resp.getText());
            StreamReader streamXML = resp.getStream();
            HtmlDocument doc = new HtmlDocument();
            doc.Load(streamXML);
            HtmlNodeCollection collection = doc.DocumentNode.SelectNodes(@".//table[@class=""mtb""]");
            HtmlNode nodeTable = collection[1];
            //--------------------------------------------------------------------
            HtmlNodeCollection trCollection = nodeTable.SelectNodes(@".//tr");
            if(trCollection==null)
            {
                Console.WriteLine("当日成交为空");
                return;
            }
            foreach (HtmlNode trNode in trCollection)
            {
                //----------------------------------------------------------------------------
                HtmlNode tdNode = trNode.SelectSingleNode(@".//td[1]");
                string Name = tdNode.InnerText.Trim();
                Console.WriteLine("证券名称:" + Name);
                //----------------------------------------------------------------------------
                tdNode = trNode.SelectSingleNode(@".//td[2]");
                string DealTime = tdNode.InnerText.Trim();
                Console.WriteLine("委托时间:" + DealTime);
                //----------------------------------------------------------------------------
                tdNode = trNode.SelectSingleNode(@".//td[3]");
                string DealType = tdNode.InnerText.Trim();
                Console.WriteLine("买卖标志:" + DealType);
                //----------------------------------------------------------------------------
                tdNode = trNode.SelectSingleNode(@".//td[4]");
                string DealPrice = tdNode.InnerText.Trim();
                Console.WriteLine("成交价格:" + DealPrice);
                //----------------------------------------------------------------------------
                tdNode = trNode.SelectSingleNode(@".//td[5]");
                string DealNum = tdNode.InnerText.Trim();
                Console.WriteLine("成交数量:" + DealNum);
                //----------------------------------------------------------------------------
                tdNode = trNode.SelectSingleNode(@".//td[6]");
                string DealAmount = tdNode.InnerText.Trim();
                Console.WriteLine("成交金额:" + DealAmount);
                //----------------------------------------------------------------------------
                tdNode = trNode.SelectSingleNode(@".//td[7]");
                string DealNo = tdNode.InnerText.Trim();
                Console.WriteLine("成交编号:" + DealNo);
                //----------------------------------------------------------------------------
                tdNode = trNode.SelectSingleNode(@".//td[8]");
                string OrderNo = tdNode.InnerText.Trim();
                Console.WriteLine("委托编号:" + OrderNo);
                //----------------------------------------------------------------------------
                tdNode = trNode.SelectSingleNode(@".//td[9]");
                string Code = tdNode.InnerText.Trim();
                Console.WriteLine("证券代码:" + Code);
                //----------------------------------------------------------------------------
                tdNode = trNode.SelectSingleNode(@".//td[10]");
                string CustomCode = tdNode.InnerText.Trim();
                Console.WriteLine("股东代码:" + CustomCode);


            }
        }

        public Dictionary<string, double> Price(string Code)
        {
            string url = @"https://trade.foundersc.com/ajax/content_DbDs,WebWt_fzgj.ashx?_method=bind&_session=r";
            string postData = "gpdm=" + Code;
            Response resp = r.Post(url, postData, ref cookies);
            string str = resp.getText().Replace("\\", "");
            Dictionary<string, double> priceDict = new Dictionary<string, double>();
            
            Regex regex= new Regex(@"'([a-zA-Z0-9]+)':'([0-9\.]+)'[,\}]");
            MatchCollection matchCollection = regex.Matches(str);
            foreach(Match match in matchCollection)
            {
                priceDict.Add(match.Groups[1].Value,Convert.ToDouble(match.Groups[2].Value));
               // Console.WriteLine(match.Groups[1].Value + " : " + match.Groups[2].Value);
            }
            return priceDict;



        }

        public Dictionary<string, string> StockInfo(string Code,string tradeType)
        {
            string url = "";
            string postData = "";
            Response resp = null;
            Dictionary<string, string> stockinfo = new Dictionary<string, string>();
            if (tradeType=="B")
            {
                url = "https://trade.foundersc.com/ajax/Buy,WebWt_fzgj.ashx?_method=Getgp&_session=r";
                postData = postData = "gpdm=" + Code + "\nscwttype=0";
            }
            else if(tradeType == "S")
            {
                url = "https://trade.foundersc.com/ajax/content_Sale,WebWt_fzgj.ashx?_method=Getgp&_session=r";
                postData = "gpdm="+Code+"\nscwttype=0";
            }
            else
            {
                //Console.WriteLine("买卖标志输入错误，应为 B 或者 S ");
                stockinfo.Add("result", "false");
                stockinfo.Add("err", "买卖标志输入错误，应为 B 或者 S ");
                return stockinfo; 
            }
             resp = r.Post(url, postData,ref cookies);
             string str = resp.getText().Replace("\\", "");
            Regex regex = new Regex(@"'([a-zA-Z0-9]+)':'(.*?)'[,\}]");
            MatchCollection matchCollection = regex.Matches(str);
            foreach (Match match in matchCollection)
            {
                stockinfo.Add(match.Groups[1].Value, match.Groups[2].Value);
                 //Console.WriteLine(match.Groups[1].Value + " : " + match.Groups[2].Value);
            }
            stockinfo.Add("result", "true");
            return stockinfo;
        }

    }
}