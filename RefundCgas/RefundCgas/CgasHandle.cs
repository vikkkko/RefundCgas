using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using ThinNeo;

namespace RefundCgas
{
    class CgasHandle
    {
        public static void getLockUtxoByAddress(string address="")
        {
            byte[] postdata;

            String url="";
            if(address == "")
                url= httpHelper.MakeRpcUrlPost(Program.WalletApi, "getCagsLockUtxo", out postdata);
            else
                url=httpHelper.MakeRpcUrlPost(Program.WalletApi, "getCagsLockUtxo", out postdata,new JValue(address));
            var result = httpHelper.HttpPost(url, postdata);
            JObject joResult = JObject.Parse(result);
            if (joResult.ContainsKey("error"))
            {
                Log.Error(joResult.ToString());
                return;
            }
            JArray jaresult = (JArray)joResult["result"];
            for (var i = 0; i < jaresult.Count; i++)
            {
                Log.Common_Green(jaresult[i].ToString());
                Log.Common_white("");
            }
        }

        public static void RefundCgas()
        {
            byte[] postdata;
            var url = httpHelper.MakeRpcUrlPost(Program.WalletApi, "getCagsLockUtxo", out postdata);
            var result = httpHelper.HttpPost(url, postdata);
            JObject joResult = JObject.Parse(result);
            if (joResult.ContainsKey("error"))
            {
                Log.Error(joResult.ToString());
                return;
            }
            JArray jaresult = (JArray)joResult["result"];
            for (var i = 0; i < jaresult.Count; i++)
            {
                JObject data = (JObject)jaresult[i];
                Log.Common_Green("正在找回txid:"+ data["txid"].ToString()+"中地址:"+ data["lockAddress"].ToString()+"的"+ data["value"].ToString()+"gas");
                Utxo utxo = new Utxo(data["lockAddress"].ToString(),new Hash256(data["txid"].ToString()), "0x602c79718b16e442de58778e148d0b1084e3b2dffd5de6b7b16cee7969282de7",decimal.Parse(data["value"].ToString()), 0);
                TranGas(new List<Utxo>() { utxo }, data["lockAddress"].ToString(), decimal.Parse(data["value"].ToString()));
            }
        }

        private static void TranGas(List<Utxo> list,string address, decimal value)
        {
            var tran = makeTran(list, address, new ThinNeo.Hash256("0x602c79718b16e442de58778e148d0b1084e3b2dffd5de6b7b16cee7969282de7"), value);
            tran.type = ThinNeo.TransactionType.ContractTransaction;
            tran.version = 0;
            //sign and broadcast
            //做智能合约的签名
            byte[] sgasScript = null;
            byte[] postdata;
            var url = httpHelper.MakeRpcUrlPost(Program.BlockApi, "getcontractstate",out postdata, new JValue("0x74f2dc36a68fdc4682034178eb2220729231db76"));
            var result = httpHelper.HttpPost(url, postdata);
            var _json = JObject.Parse(result);
            var _resultv = ((JArray)_json["result"])[0];
            sgasScript = ThinNeo.Helper.HexString2Bytes(_resultv["script"].ToString());
            byte[] iscript = null;
            using (var sb = new ThinNeo.ScriptBuilder())
            {
                sb.EmitPushNumber(0);
                sb.EmitPushNumber(0);
                iscript = sb.ToArray();
            }
            tran.AddWitnessScript(sgasScript, iscript);

            var trandata = tran.GetRawData();
            var strtrandata = ThinNeo.Helper.Bytes2HexString(trandata);

            url = httpHelper.MakeRpcUrlPost(Program.BlockApi, "sendrawtransaction", out postdata, new JValue(strtrandata));

            result = httpHelper.HttpPost(url, postdata);
            Log.Common_Green("得到的结果是：" + result);
        }


        private static ThinNeo.Transaction makeTran(List<Utxo> utxos, string targetaddr, ThinNeo.Hash256 assetid, decimal sendcount, decimal extgas = 0, List<Utxo> utxos_ext = null, string extaddr = null)
        {
            var tran = new ThinNeo.Transaction();
            tran.type = ThinNeo.TransactionType.ContractTransaction;
            if (extgas >= 1)
            {
                tran.version = 1;//0 or 1
            }
            else
            {
                tran.version = 0;//0 or 1
            }
            tran.extdata = null;

            tran.attributes = new ThinNeo.Attribute[0];
            var scraddr = "";
            utxos.Sort((a, b) =>
            {
                if (a.value > b.value)
                    return 1;
                else if (a.value < b.value)
                    return -1;
                else
                    return 0;
            });
            decimal count = decimal.Zero;
            List<ThinNeo.TransactionInput> list_inputs = new List<ThinNeo.TransactionInput>();
            for (var i = 0; i < utxos.Count; i++)
            {
                ThinNeo.TransactionInput input = new ThinNeo.TransactionInput();
                input.hash = utxos[i].txid;
                input.index = (ushort)utxos[i].n;
                list_inputs.Add(input);
                count += utxos[i].value;
                scraddr = utxos[i].addr;
                if (count >= sendcount)
                {
                    break;
                }
            }
            decimal count_ext = decimal.Zero;
            if (utxos_ext != null)
            {
                //手续费
                ThinNeo.TransactionInput input = new ThinNeo.TransactionInput();
                input.hash = utxos_ext[0].txid;
                input.index = (ushort)utxos_ext[0].n;
                count_ext = utxos_ext[0].value;
                list_inputs.Add(input);
            }

            tran.inputs = list_inputs.ToArray();
            if (count >= sendcount)//输入大于等于输出
            {
                List<ThinNeo.TransactionOutput> list_outputs = new List<ThinNeo.TransactionOutput>();
                //输出
                if (sendcount > decimal.Zero && targetaddr != null)
                {
                    ThinNeo.TransactionOutput output = new ThinNeo.TransactionOutput();
                    output.assetId = assetid;
                    output.value = sendcount;
                    output.toAddress = ThinNeo.Helper.GetPublicKeyHashFromAddress(targetaddr);
                    list_outputs.Add(output);
                }
                var change = count - sendcount - extgas;
                decimal extchange = decimal.Zero;
                //找零
                if (utxos_ext != null)
                {
                    change = count - sendcount;
                    extchange = count_ext - extgas;
                }
                else
                {
                    change = count - sendcount - extgas;
                }
                if (change > decimal.Zero)
                {
                    ThinNeo.TransactionOutput outputchange = new ThinNeo.TransactionOutput();
                    outputchange.toAddress = ThinNeo.Helper.GetPublicKeyHashFromAddress(scraddr);
                    outputchange.value = change;
                    outputchange.assetId = assetid;
                    list_outputs.Add(outputchange);
                }
                if (extchange > decimal.Zero)
                {
                    ThinNeo.TransactionOutput outputchange = new ThinNeo.TransactionOutput();
                    outputchange.toAddress = ThinNeo.Helper.GetPublicKeyHashFromAddress(extaddr);
                    outputchange.value = extchange;
                    outputchange.assetId = assetid;
                    list_outputs.Add(outputchange);
                }
                tran.outputs = list_outputs.ToArray();
            }
            else
            {
                throw new Exception("no enough money.");
            }
            return tran;
        }

    }



    public class Utxo
    {
        //txid[n] 是utxo的属性
        public Hash256 txid;
        public int n;

        //asset资产、addr 属于谁，value数额，这都是查出来的
        public string addr;
        public string asset;
        public decimal value;
        public Utxo(string _addr, Hash256 _txid, string _asset, decimal _value, int _n)
        {
            this.addr = _addr;
            this.txid = _txid;
            this.asset = _asset;
            this.value = _value;
            this.n = _n;
        }
    }
}
