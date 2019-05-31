using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using ThinNeo;

namespace RefundCgas
{
    class RefundSellContract
    {

        public static void RefundLoop()
        {
            var JAData = mongoHelper.GetData(Program.conn, Program.db, Program.coll,"{register:\"0xfe041f87b1a4cc0efb827664d6f20a0e990d0969\"}");
            for (var i = 0; i < JAData.Count; i++)
            {
                MyJson.JsonNode_Object json = JAData[i].AsDict();
                var str_balance = json["balance"].AsDict()["$numberDecimal"].AsString();
                BigInteger balance = (BigInteger)(decimal.Parse(str_balance,System.Globalization.NumberStyles.Float) * 100000000);
                string address = json["address"].AsString();

                if (balance <= 0)
                    continue;
                byte[] data = null;
                //MakeTran
                ThinNeo.Transaction tran = new Transaction();
                {
                    using (ScriptBuilder sb = new ScriptBuilder())
                    {
                        MyJson.JsonNode_Array array = new MyJson.JsonNode_Array();

                        byte[] randombytes = new byte[32];
                        using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
                        {
                            rng.GetBytes(randombytes);
                        }
                        BigInteger randomNum = new BigInteger(randombytes);
                        sb.EmitPushNumber(randomNum);
                        sb.Emit(ThinNeo.OpCode.DROP);
                        array.AddArrayValue("(addr)" + address);//from
                        array.AddArrayValue("(int)" + balance);//value

                        sb.EmitParamJson(array);
                        sb.EmitPushString("getmoneyback");
                        sb.EmitAppCall(new Hash160("0xfe041f87b1a4cc0efb827664d6f20a0e990d0969"));
                        data = sb.ToArray();
                    }
                    //sign and broadcast
                    var wif = "KwZih114osBp58RwpEn4ZAcEcCTLP6yMAhdikb6oPRxqgvWpcqF1";
                    var prikey = ThinNeo.Helper.GetPrivateKeyFromWIF(wif);
                    var pubkey = ThinNeo.Helper.GetPublicKeyFromPrivateKey(prikey);
                    var scriptHash = ThinNeo.Helper.GetScriptHashFromPublicKey(pubkey);
                    var addressAdmin = ThinNeo.Helper.GetAddressFromPublicKey(pubkey);

                    tran.type = ThinNeo.TransactionType.InvocationTransaction;
                    var idata = new ThinNeo.InvokeTransData();
                    tran.extdata = idata;
                    idata.script = data;
                    idata.gas = 0;
                    tran.inputs = new ThinNeo.TransactionInput[0];
                    tran.outputs = new ThinNeo.TransactionOutput[0];
                    tran.attributes = new ThinNeo.Attribute[1];
                    tran.attributes[0] = new ThinNeo.Attribute();
                    tran.attributes[0].usage = TransactionAttributeUsage.Script;
                    tran.attributes[0].data = ThinNeo.Helper.GetPublicKeyHashFromAddress(addressAdmin);

                    var signdata = ThinNeo.Helper.Sign(tran.GetMessage(), prikey);

                    tran.AddWitness(signdata, pubkey, addressAdmin);
                    var trandata = tran.GetRawData();
                    var strtrandata = ThinNeo.Helper.Bytes2HexString(trandata);
                    byte[] postdata;
                    var url = httpHelper.MakeRpcUrlPost(Program.BlockApi, "sendrawtransaction", out postdata, new JValue(strtrandata));
                    var result = httpHelper.HttpPost(url, postdata);
                    Console.WriteLine(result);
                    var j_result = MyJson.Parse(result).AsDict()["result"].AsList()[0].AsDict();
                    if (j_result["sendrawtransactionresult"].AsBool())
                    {
                        var txid = j_result["txid"].AsString();
                        Record record = new Record();
                        record.txid = txid;
                        record.addr = address;
                        record.value = balance.ToString();
                        mongoHelper.InsetOne<Record>(Program.conn, Program.db, Program.collBak, record);
                    }
                    else
                    {

                    }
                }
            }
        }
    }

    public class Record
    {
        public string txid;
        public string addr;
        public string value;
    }
}
