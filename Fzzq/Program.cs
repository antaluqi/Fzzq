using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fzzq;

namespace FzzqProgram
{
    class Program
    {
        static void Main(string[] args)
        {
            string username = "1020001525";
            string passpord = "615919";

            FzzqAPI api = new FzzqAPI(username, passpord);
            string reStr = "";

            if(api.LoginFromCookie(ref reStr))
           // if (api.Login(ref reStr))
            {
                Dictionary<string, string>r= api.StockInfo("600118", "B");
                Console.WriteLine(r["result"].ToString());

            }
            Console.WriteLine(username+reStr);
            Console.ReadKey();
        }
    }
}
