using System;
using System.Collections.Generic;
using System.IO;
using YamlDotNet.Serialization;

public class ConfigData
{
    public int timeout { get; set; }
    public bool startup { get; set; }
    public bool debug { get; set; }
    public int wait { get; set; }

    public List<Dictionary<string, string>> settings { get; set; }  // 改成字符串key和value
    public List<Dictionary<string, string>> exit { get; set; }  // 改成字符串key和value
}

public static class YamlConfigLoader
{
    public static ConfigData LoadConfig()
    {
        string exePath = AppDomain.CurrentDomain.BaseDirectory;
        string yamlPath = Path.Combine(exePath, "config.yaml");

        if (!File.Exists(yamlPath))
        {
            throw new FileNotFoundException("未找到配置文件: config.yaml");
        }

        string yaml = File.ReadAllText(yamlPath);

        var deserializer = new DeserializerBuilder().Build();
        var result = deserializer.Deserialize<Dictionary<string, object>>(yaml);

        var config = new ConfigData
        {
            timeout = Convert.ToInt32(result["timeout"]),
            startup = Convert.ToBoolean(result["startup"]),
            debug = Convert.ToBoolean(result["debug"]),
            wait = Convert.ToInt32(result["wait"]),
            settings = new List<Dictionary<string, string>>(),
            exit = new List<Dictionary<string, string>>()
        };

        // 解析 settings
        if (result.ContainsKey("settings"))
        {
            var settingsList = result["settings"] as List<object>;
            foreach (var item in settingsList)
            {
                var entry = item as Dictionary<object, object>;
                var dict = new Dictionary<string, string>();
                foreach (var kv in entry)
                {
                    dict[kv.Key.ToString()] = kv.Value.ToString();
                }
                config.settings.Add(dict);
            }
        }

        // 解析 exit
        if (result.ContainsKey("exit"))
        {
            var exitList = result["exit"] as List<object>;
            foreach (var item in exitList)
            {
                var entry = item as Dictionary<object, object>;
                var dict = new Dictionary<string, string>();
                foreach (var kv in entry)
                {
                    dict[kv.Key.ToString()] = kv.Value.ToString();
                }
                config.exit.Add(dict);
            }
        }

        return config;
    }

}