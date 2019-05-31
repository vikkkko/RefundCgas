using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Text;

namespace RefundCgas
{
    class mongoHelper
    {
        public static int GetMaxIndex(string mongodbConnStr, string mongodbDatabase, string collName, string key, string findFliter = "{}")
        {
            var client = new MongoClient(mongodbConnStr);
            var database = client.GetDatabase(mongodbDatabase);
            var collection = database.GetCollection<BsonDocument>(collName);
            var data = collection.Find(BsonDocument.Parse(findFliter)).Sort(BsonDocument.Parse("{" + key + ":-1}")).Limit(1).ToList();
            client = null;
            return data.Count == 0 ? -1 : (int)data[0][key];
        }

        //存入某个数据
        public static void InsetOne<T>(string mongodbConnStr, string mongodbDatabase, string collName, T value)
        {
            var client = new MongoClient(mongodbConnStr);
            var database = client.GetDatabase(mongodbDatabase);
            var collection = database.GetCollection<T>(collName);
            try
            {
                collection.InsertOne(value);
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(e);
                Console.ForegroundColor = ConsoleColor.White;
            }
            client = null;
        }

        public static BsonDocument FindOne(string mongodbConnStr, string mongodbDatabase, string collName, string findFliter)
        {
            var client = new MongoClient(mongodbConnStr);
            var database = client.GetDatabase(mongodbDatabase);
            var collection = database.GetCollection<BsonDocument>(collName);
            try
            {
                List<BsonDocument> query = collection.Find(BsonDocument.Parse(findFliter)).Limit(1).ToList();
                client = null;
                if (query.Count > 0)
                {

                    return query[0];
                }
                else
                {
                    return null;
                }
            }
            catch (Exception e)
            {
                return FindOne(mongodbConnStr, mongodbDatabase, collName, findFliter);
            }
        }

        public static void ReplaceData(string mongodbConnStr, string mongodbDatabase, string collName, string whereFliter, string replaceFliter)
        {
            var client = new MongoClient(mongodbConnStr);
            var database = client.GetDatabase(mongodbDatabase);
            var collection = database.GetCollection<BsonDocument>(collName);
            try
            {
                List<BsonDocument> query = collection.Find(whereFliter).ToList();
                if (query.Count == 0)//表示并没有数据
                {
                    client = null;
                    InsetOne(mongodbConnStr, mongodbDatabase, collName, replaceFliter);
                }
                else
                {
                    //collection.DeleteOne(whereFliter);
                    collection.ReplaceOne(BsonDocument.Parse(whereFliter), BsonDocument.Parse(replaceFliter));
                    client = null;
                }
            }
            catch (Exception e)
            {
                ReplaceData(mongodbConnStr, mongodbDatabase, collName, whereFliter, replaceFliter);
            }


        }

        public static MyJson.JsonNode_Array GetData(string mongodbConnStr, string mongodbDatabase, string coll, string findFliter)
        {
            var client = new MongoClient(mongodbConnStr);
            var database = client.GetDatabase(mongodbDatabase);
            var collection = database.GetCollection<BsonDocument>(coll);

            List<BsonDocument> query = collection.Find(BsonDocument.Parse(findFliter)).ToList();
            client = null;

            if (query.Count > 0)
            {
                var jsonWriterSettings = new JsonWriterSettings { OutputMode = JsonOutputMode.Strict };
                MyJson.JsonNode_Array JA = MyJson.Parse(query.ToJson(jsonWriterSettings)) as MyJson.JsonNode_Array;
                foreach (MyJson.JsonNode_Object j in JA)
                {
                    j.Remove("_id");
                }
                return JA;
            }
            else { return new MyJson.JsonNode_Array(); }
        }



        public static void DelData(string mongodbConnStr, string mongodbDatabase, string coll, string findFliter)
        {
            var client = new MongoClient(mongodbConnStr);
            var database = client.GetDatabase(mongodbDatabase);
            var collection = database.GetCollection<BsonDocument>(coll);
            var query = collection.DeleteMany(BsonDocument.Parse(findFliter));
            client = null;
        }

        public static long GetDataCount(string mongodbConnStr, string mongodbDatabase, string coll, string findFliter = "{}")
        {
            var client = new MongoClient(mongodbConnStr);
            var database = client.GetDatabase(mongodbDatabase);
            var collection = database.GetCollection<BsonDocument>(coll);
            var txCount = collection.CountDocuments(findFliter);
            client = null;

            return txCount;
        }


        public static int Getblockheight(string mongodbConnStr, string mongodbDatabase, string coll)
        {
            var client = new MongoClient(mongodbConnStr);
            var database = client.GetDatabase(mongodbDatabase);
            var collection = database.GetCollection<BsonDocument>(coll);
            var sortBson = BsonDocument.Parse("{index:-1}");
            var query = collection.Find(new BsonDocument()).Sort(sortBson).Limit(1).ToList();
            if (query.Count > 0)
            { return (int)query[0]["index"]; }
            return 0;
        }

        public static int GetNEP5transferheight(string mongodbConnStr, string mongodbDatabase, string coll)
        {
            var client = new MongoClient(mongodbConnStr);
            var database = client.GetDatabase(mongodbDatabase);
            var collection = database.GetCollection<BsonDocument>(coll);
            var sortBson = BsonDocument.Parse("{blockindex:-1}");
            var query = collection.Find(new BsonDocument()).Sort(sortBson).Limit(1).ToList();
            if (query.Count > 0)
            { return (int)query[0]["blockindex"]; }
            return 0;
        }

        public static MyJson.JsonNode_Array GetDataPages(string mongodbConnStr, string mongodbDatabase, string coll, string sortStr, int pageCount, int pageNum, string findBson = "{}")
        {
            var client = new MongoClient(mongodbConnStr);
            var database = client.GetDatabase(mongodbDatabase);
            var collection = database.GetCollection<BsonDocument>(coll);

            List<BsonDocument> query = collection.Find(findBson).Sort(sortStr).Skip(pageCount * (pageNum - 1)).Limit(pageCount).ToList();
            client = null;

            if (query.Count > 0)
            {

                var jsonWriterSettings = new JsonWriterSettings { OutputMode = JsonOutputMode.Strict };
                MyJson.JsonNode_Array JA = MyJson.Parse(query.ToJson(jsonWriterSettings)) as MyJson.JsonNode_Array;
                foreach (MyJson.JsonNode_Object j in JA)
                {
                    j.Remove("_id");
                }
                return JA;
            }
            else { return new MyJson.JsonNode_Array(); }
        }

        public static void test(string mongodbConnStr, string mongodbDatabase, string collName, string[] values)
        {
            var client = new MongoClient(mongodbConnStr);
            var database = client.GetDatabase(mongodbDatabase);
            var collection = database.GetCollection<BsonDocument>(collName);
            collection.ReplaceOne(BsonDocument.Parse("{}"), BsonDocument.Parse("{txid:1}"));
        }

        public static void Commit(string mongodbConnStr, string mongodbDatabase, string collName, string[] values)
        {
            var client = new MongoClient(mongodbConnStr);
            var database = client.GetDatabase(mongodbDatabase);
            var collection = database.GetCollection<BsonDocument>(collName);
            var session = client.StartSession();
            session.StartTransaction();
            try
            {
                collection.InsertOne(session,BsonDocument.Parse(values[0]));
                collection.InsertOne(session,BsonDocument.Parse(values[1]));
                collection.InsertOne(session,BsonDocument.Parse(values[2]));
                collection.InsertOne(BsonDocument.Parse(values[110]));
                session.CommitTransaction();
            }
            catch (Exception e)
            {
                Console.WriteLine("3~~~~");
                session.AbortTransaction();
            }

        }

    }
}
