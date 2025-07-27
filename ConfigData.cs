using System;
using System.Collections.Generic;
using System.IO;
using YamlDotNet.Serialization;

public class ConfigData
{
    public int timeout { get; set; }
    public bool startup { get; set; }
    public int ec { get; set; }
    public int data { get; set; }

    public List<Dictionary<int, int>> settings { get; set; }
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
            ec = Convert.ToInt32(result["ec"]),
            data = Convert.ToInt32(result["data"]),
            settings = new List<Dictionary<int, int>>()
        };

        var settingsList = result["settings"] as List<object>;

        foreach (var item in settingsList)
        {
            var entry = item as Dictionary<object, object>;
            var dict = new Dictionary<int, int>();
            foreach (var kv in entry)
            {
                dict[Convert.ToInt32(kv.Key)] = Convert.ToInt32(kv.Value);
            }
            config.settings.Add(dict);
        }

        return config;
    }
}