using System;
using System.IO;
using System.Xml;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
namespace xmlToJson
{
    class ConvertXmlMsKeysToJson
    {
        static void Main(string[] args)
        {
            try
            {
                if (args == null || string.IsNullOrEmpty(args[0]) || !Directory.Exists(args[0]))
                {
                    Console.WriteLine("file is not exist");
                    return;
                }

                //var keys = new List<Key>();
                var keysd = new Dictionary<string, List<string>>();
                var keysd2 = new Dictionary<string, List<string>>();
                var keysd1 = new Dictionary<string, string>();
                var keysSets = new List<KeysSet>();
                //var keyFiles = new List<KeysFile>();
                var folder = args[0];
                var direcoryInfo = new DirectoryInfo(folder);

                foreach (var fileInfo in direcoryInfo.GetFiles("*.xml", SearchOption.AllDirectories))
                {
                        var file = fileInfo.FullName;
                    try
                    {
                        var yourKey = ReadXmlFile(file);

                        yourKey.Product_Key.ForEach(p =>
                        {
                            p.Key.ForEach(k =>
                            {
                                if (!string.IsNullOrWhiteSpace(k.Text) && !k.Text.Contains("<") && !k.Text.Contains("key"))
                                {
                                    var thisKey = k.Text.Replace("\r\n", "").Trim();
                                    var thisName = p.Name.Replace("\r\n", "").Trim();
                                    if (!keysd.ContainsKey(thisKey))
                                    {
                                        keysd.Add(thisKey, new List<string> { thisName });
                                    }
                                    else if (!keysd[thisKey].Contains(thisName))
                                    {
                                        keysd[thisKey].Add(thisName);
                                    }
                                }
                            });
                        });
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"{file} {e.Message}");
                    }
                }


             

                foreach (var k in keysd)
                {
                    keysd1.Add(k.Key, string.Join("|", k.Value));
                }

                foreach (var k in keysd1)
                {
                    if (!keysd2.ContainsKey(k.Value))
                        keysd2.Add(k.Value, new List<string> { k.Key });
                    else
                        keysd2[k.Value].Add(k.Key);
                }


                keysd2 = keysd2.OrderBy(x => x.Key).ToDictionary(d => d.Key, d => d.Value);

                foreach (var k in keysd2)
                {
                    keysSets.Add(new KeysSet
                    {
                        Products = k.Key.Split('|').ToList(),
                        Keys = k.Value
                    });
                }
                //var j3 = JsonConvert.SerializeObject(keysd2, Newtonsoft.Json.Formatting.Indented);
                var j4 = JsonConvert.SerializeObject(keysSets, Newtonsoft.Json.Formatting.Indented);
                var jsonFile = direcoryInfo.FullName + "\\keys.json";
                if (File.Exists(jsonFile))
                    File.Delete(jsonFile);
                File.WriteAllText(jsonFile, j4);
                Console.WriteLine($"file exported and saved as {jsonFile}");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }


        static YourKey ReadXFile(string file)
        {
            try
            {
                var yourKey = new YourKey();
                var lines = file.Split('\n');
                yourKey.Product_Key = new List<Product_Key>();
                foreach (var line in lines)
                {
                    var segments = line.Replace("\r", "").Split('\t');
                    var p = new Product_Key
                    {
                        Name = segments[1],
                        Key = new List<Key> {new Key {Text = segments[0]}}
                    };
                    yourKey.Product_Key.Add(p);
                }

                return yourKey;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return null;
            }
        }

        static YourKey ReadXmlFile(string file)
        {
            var fileContent = File.ReadAllText(file);
            if (string.IsNullOrWhiteSpace(fileContent))
            {
                Console.WriteLine($"{file} Content is empty");
                return null;
            }

            if (file.EndsWith("\\x10x.xml"))
                return ReadXFile(fileContent);

            var doc = new XmlDocument();
            doc.LoadXml(fileContent);

            var json = JsonConvert.SerializeXmlNode(doc, Newtonsoft.Json.Formatting.Indented, false);

            if (string.IsNullOrWhiteSpace(json))
            {
                Console.WriteLine($"{file} after JsonConvert, JSON content is empty");
                return null;
            }

            json = json.Replace("@", "").Replace("#", "");

            var keysFile = JsonConvert.DeserializeObject<KeysFile>(json);
            if (keysFile?.root?.YourKey != null)
            {
                if (keysFile?.root?.YourKey?.Product_Key != null && keysFile.root.YourKey.Product_Key.Any())
                    return keysFile.root?.YourKey;
            }
            else if (keysFile?.root?.YourKey == null)
            {
                var yourKey = JsonConvert.DeserializeObject<Root>(json);
                if (yourKey?.YourKey?.Product_Key != null && yourKey.YourKey.Product_Key.Any())
                {
                    return yourKey.YourKey;
                }
            }
           
            Console.WriteLine($"{file} does not contains any keys or could not Deserialize it");
            return null;
        }
    }

    public class SingleValueArrayConverter<T> : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            object retVal = new Object();
            if (reader.TokenType == JsonToken.StartObject)
            {
                T instance = (T)serializer.Deserialize(reader, typeof(T));
                retVal = new List<T>() { instance };
            }
            else if (reader.TokenType == JsonToken.StartArray)
            {
                retVal = serializer.Deserialize(reader, objectType);
            }
            return retVal;
        }

        public override bool CanConvert(Type objectType)
        {
            return true;
        }
    }

    public class Key
    {
        public string Name { get; set; }
        public string ID { get; set; }
        public string Type { get; set; }
        public string ClaimedDate { get; set; }
        public string Text { get; set; }
    }

    public class Product_Key
    {
        [JsonConverter(typeof(SingleValueArrayConverter<Key>))]
        public List<Key> Key { get; set; }
        public string Name { get; set; }
        public string KeyRetrievalNote { get; set; }
    }

    public class YourKey
    {
        public List<Product_Key> Product_Key { get; set; }
    }

    public class KeysFile
    {
        public Root root { get; set; }
    }

    public class Root
    {
        public YourKey YourKey { get; set; }
    }

    public class KeysSet
    {
        public List<string> Products { get; set; }
        public List<string> Keys { get; set; }
    }
}
