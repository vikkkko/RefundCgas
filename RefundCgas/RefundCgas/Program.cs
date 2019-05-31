using System;

namespace RefundCgas
{
    class Program
    {
        public static string WalletApi = "https://apiwallet.nel.group/api/mainnet";
        public static string BlockApi = "https://api.nel.group/api/mainnet";
        public static string conn = "mongodb://notifyData_mainnet:NELqingmingzi3345@dds-bp1b36419665fdd41167-pub.mongodb.rds.aliyuncs.com:3717,dds-bp1b36419665fdd42489-pub.mongodb.rds.aliyuncs.com:3717/contractNotifyInfo_mainnet_New?replicaSet=mgset-4977005";
        public static string db = "contractNotifyInfo_mainnet_New";
        public static string coll = "cgasBalanceState";
        public static string collBak = "cgasBalanceStateBak";

        public static string WalletApi_test = "https://apiwallet.nel.group/api/testnet";
        public static string BlockApi_test = "https://api.nel.group/api/testnet";
        public static string conn_test = "mongodb://notifyDataStorage:NELqingmingzi1128@dds-bp1b36419665fdd41167-pub.mongodb.rds.aliyuncs.com:3717,dds-bp1b36419665fdd42489-pub.mongodb.rds.aliyuncs.com:3717/contractNotifyInfo?replicaSet=mgset-4977005";
        public static string db_test = "contractNotifyInfo";


        public static string SnapshotConn = "mongodb://snapshot_mainnet:NELqingmingzi1128@dds-bp1b36419665fdd41167-pub.mongodb.rds.aliyuncs.com:3717,dds-bp1b36419665fdd42489-pub.mongodb.rds.aliyuncs.com:3717/SnapshotAndAirdrop_mainnet?replicaSet=mgset-4977005";
        public static string SnapshotDb = "SnapshotAndAirdrop_mainnet";
        public static string SnapshotColl = "Snapshot_NNC_2908500";

        public static string AnalysisConn = "mongodb://nelData_mainnet:NELqingmingzi3345@dds-bp1df57f935202e41897-pub.mongodb.rds.aliyuncs.com:3717,dds-bp1df57f935202e42907-pub.mongodb.rds.aliyuncs.com:3717/NeoBlockData_mainnet?replicaSet=mgset-10445701";
        public static string AnalysisDb = "NeoBlockData_mainnet";
        public static string AnalysisColl = "NEP5transfer";

        public static string TestConn = "mongodb://18.218.102.126:33787/GoldBox";
        public static string TestDb = "GoldBox";
        public static string TestColl = "GoldBox";

        static void Main(string[] args)
        {
            showChangeNet();
            var net = Console.ReadLine();
            if (net != "2")
            {
                WalletApi = WalletApi_test;
                BlockApi = BlockApi_test;
                conn = conn_test;
                db = db_test;
            }
            showMenu();
            while (true)
            {
                try
                {
                    var instruct = Console.ReadLine();
                    if (instruct == "?" || instruct == "？")
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
                    else if (instruct == "3")
                    {
                        RefundSellContract.RefundLoop();
                    }
                    else if (instruct == "4")
                    {
                        test4.JustTest();
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

        static void showChangeNet()
        {
            Log.Common_white("选择主网或者测试网   1是测试网  2是主网");
        }
        static void showMenu()
        {
            Log.Common_white("数据由NEL提供");
            Log.Common_white("当你输入？时重新显示这些提示");
            Log.Common_white("输入 1 查询某个地址是否有被锁的cgas没有退回");
            Log.Common_white("输入 2 将所有的被锁的CGAS返还给所有者");
            Log.Common_white("输入3 退回拍卖合约中所有人的cags");
        }

    }
}
