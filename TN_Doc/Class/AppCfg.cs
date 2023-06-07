using System.IO;
using Newtonsoft.Json;
using System.Text;

namespace TN_Doc.Class
{
    class cfgFileRW
    {
        //Сохранить файл конфигурации
        public static bool SaveCfg(string path, string FileName, object st)
        {
            DirectoryInfo dirInfo = new DirectoryInfo(path);
            if (!dirInfo.Exists) dirInfo.Create();

            string PathFileName = path + FileName;
            if (File.Exists(PathFileName)) File.Delete(PathFileName);
            File.WriteAllText(PathFileName, JsonConvert.SerializeObject(st), Encoding.UTF8);

            return true;
        }

        //Загрузить файл конфигурации
        public static bool LoadCfg<T>(string path, ref T st)
        {
            if (File.Exists(path))
            {
                using (StreamReader file = new StreamReader(File.Open(path, FileMode.Open), Encoding.UTF8))
                {
                    JsonSerializer serializer = new JsonSerializer();
                    st = (T)serializer.Deserialize(file, typeof(T));
                    return true;
                }
            }

            return false;
        }
    }
}
