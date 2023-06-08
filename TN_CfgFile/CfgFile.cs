using System;
using System.IO;
using Newtonsoft.Json;
using System.Text;

namespace TN_CfgFile
{
    [Obsolete]
    public class CfgFile
    {
        public static bool SaveCfg(string path, string fileName, object obj)
        {
            DirectoryInfo dirInfo = new DirectoryInfo(path);
            if (!dirInfo.Exists) dirInfo.Create();

            string PathFileName = path + fileName;
            if (File.Exists(PathFileName)) File.Delete(PathFileName);
            File.WriteAllText(PathFileName, JsonConvert.SerializeObject(obj), Encoding.UTF8);
            return true;
        }
        
        public static bool LoadCfg<T>(string path, ref T obj)
        {
            if (File.Exists(path))
            {
                using (StreamReader file = new StreamReader(File.Open(path, FileMode.Open), Encoding.UTF8))
                {
                    JsonSerializer serializer = new JsonSerializer();
                    obj = (T)serializer.Deserialize(file, typeof(T));
                    return true;
                }
            }

            return false;
        }
    }
}
