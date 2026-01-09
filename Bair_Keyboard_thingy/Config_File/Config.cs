using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;


namespace Bair_Keyboard_thingy.Config_File
{
    public struct KeyboardInfo
    {
        [JsonConverter(typeof(HexIntConverter))]
        public int VendorID { get; set; }

        [JsonConverter(typeof(HexIntConverter))]
        public int ProductID { get; set; }
        public string Name { get; set; }
        public int LayerCount { get; set; }
        public bool Enabled { get; set; }

        public KeyboardInfo(int VendorID, int ProductID, string Name, int LayerCount, bool Enabled)
        {
            this.VendorID = VendorID;
            this.ProductID = ProductID;
            this.Name = Name; 
            this.LayerCount = LayerCount;
            this.Enabled = Enabled;
        }
    }

    public struct ShortcutInfo
    {

        public string Name { get; set; }
        [JsonConverter(typeof(HexIntConverter))]
        public int KeyCode { get; set; }
        public string Path { get; set; }

        public ShortcutInfo(string Name, string Path, int KeyCode)
        {
            this.Name = Name;
            this.Path = Path;
            this.KeyCode = KeyCode;
        }
    }

    public class ConfigSave
    {
        public List<KeyboardInfo> Keyboards { get; set; }
        public ConfigSave(List<KeyboardInfo> Keyboards) {
            this.Keyboards = Keyboards;
        }
        public ConfigSave() { Keyboards = new(); }
    }
    public static class Config
    {

        private const string ConfigFile = "config.json";

        public static void MakeSave(ConfigSave config)
        {
            string json = JsonConvert.SerializeObject(
                config,
                Formatting.Indented // pretty-print
            );

            File.WriteAllText(ConfigFile, json);
        }

        public static ConfigSave LoadSave()
        {
            // If no config exists, return a default one
            if (!File.Exists(ConfigFile))
            {
                return new ConfigSave();
            }

            string json = File.ReadAllText(ConfigFile);
            return JsonConvert.DeserializeObject<ConfigSave>(json)
                   ?? new ConfigSave();
        }
    }
}



public class HexIntConverter : JsonConverter<int>
{
    public override void WriteJson(JsonWriter writer, int value, JsonSerializer serializer)
    {
        // Write as hex string (e.g. "0x1A2B")
        writer.WriteValue($"0x{value:X}");
    }

    public override int ReadJson(
        JsonReader reader,
        Type objectType,
        int existingValue,
        bool hasExistingValue,
        JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.String)
        {
            string s = (string)reader.Value!;
            if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                return Convert.ToInt32(s.Substring(2), 16);

            return int.Parse(s);
        }

        return Convert.ToInt32(reader.Value);
    }
}