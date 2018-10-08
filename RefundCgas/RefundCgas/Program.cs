using System;

namespace RefundCgas
{
    class Program
    {
        public static string WalletApi = "http://localhost:60142/api/mainnet";
        public static string BlockApi = "https://api.nel.group/api/mainnet";

        static void Main(string[] args)
        {
            showMenu();
            while (true)
            {
                try
                {
                    var instruct = Console.ReadLine();
                    if (instruct == "?"|| instruct == "？")
                    {
                        showMenu();
                    }
                    else if (instruct == "1")
                    {
                        Log.Common_white("请输入你要查询的地址,不填为全查");
                        var address = Console.ReadLine();
                        if (string.IsNullOrEmpty(address))
                            CgasHandle.getLockUtxoByAddress();
                        else
                        {
                            ThinNeo.Helper.GetPublicKeyHashFromAddress(address);
                            CgasHandle.getLockUtxoByAddress(address);
                        }
                    }
                    else if (instruct == "2")
                    {
                        CgasHandle.RefundCgas();
                    }
                    else
                    {
                        Log.Error("请输入正确的指令");
                    }
                }
                catch (Exception e)
                {
                    Log.Error(e.ToString());
                }

            }

        }
        static void showMenu()
        {
            Log.Common_white("数据由NEL提供");
            Log.Common_white("当你输入？时重新显示这些提示");
            Log.Common_white("输入 1 查询某个地址是否有被锁的cgas没有退回");
            Log.Common_white("输入 2 将所有的被锁的CGAS返还给所有者");
        }

    }
}
