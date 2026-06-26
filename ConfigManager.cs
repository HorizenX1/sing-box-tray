using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace sing_box_tray
{
    public class IniFile
    {
        private readonly string _filePath;
        private readonly Dictionary<string, Dictionary<string, string>> _iniData = new(StringComparer.OrdinalIgnoreCase);

        public IniFile(string filePath)
        {
            _filePath = filePath;
            Load();
        }

        private void Load()
        {
            if (!File.Exists(_filePath)) return;
            string currentSection = "";
            foreach (var line in File.ReadLines(_filePath))
            {
                var trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith(';') || trimmed.StartsWith('#')) continue;
                if (trimmed.StartsWith('[') && trimmed.EndsWith(']'))
                {
                    currentSection = trimmed[1..^1].Trim();
                    if (!_iniData.ContainsKey(currentSection))
                        _iniData[currentSection] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                }
                else
                {
                    var parts = trimmed.Split('=', 2);
                    if (parts.Length == 2 && !string.IsNullOrEmpty(currentSection))
                    {
                        var key = parts[0].Trim();
                        var val = parts[1].Trim();
                        _iniData[currentSection][key] = val;
                    }
                }
            }
        }

        public string ReadString(string section, string key, string defaultValue = "")
        {
            if (_iniData.TryGetValue(section, out var keys) && keys.TryGetValue(key, out var val))
                return val;
            return defaultValue;
        }

        public bool ReadBool(string section, string key, bool defaultValue = false)
        {
            var s = ReadString(section, key, "");
            if (string.IsNullOrEmpty(s)) return defaultValue;
            s = s.ToLower();
            return s == "1" || s == "true" || s == "yes" || s == "on";
        }
    }

    public class TrayOptions
    {
        public string SbDir { get; set; } = "";
        public string SbConfigFile { get; set; } = "";
        public bool SystemProxyAuto { get; set; }
        public string SystemProxyBypass { get; set; } = "";
        public bool SystemProxyBypassLocal { get; set; }
        public string TunStartMode { get; set; } = "off";
        public string LogFile { get; set; } = "";
    }

    public class SingBoxConfig
    {
        public int ProxyPort { get; set; }
        public bool HasTunInbound { get; set; }
        public string JsonWithTun { get; set; } = "";
        public string JsonWithoutTun { get; set; } = "";
        public string ExternalController { get; set; } = "";
    }

    public class ConfigManager
    {
        public TrayOptions Options { get; private set; } = new();
        public SingBoxConfig SbConfig { get; private set; } = new();

        private readonly string _dataDir;

        public ConfigManager(string dataDir)
        {
            _dataDir = dataDir;
            Reload();
        }

        public void Reload()
        {
            LoadOptions();
            LoadSingBoxConfig();
        }

        private void LoadOptions()
        {
            var iniPath = Path.Combine(_dataDir, "tray-config.ini");
            if (!File.Exists(iniPath))
            {
                // 创建一个默认的 ini 文件
                try
                {
                    File.WriteAllText(iniPath, @"[tray-config]
sb-dir = 
sb-config-file = config.json
tun-start-mode = off
system-proxy-auto = 0
system-proxy-bypass = 
system-proxy-bypass-local = 0
log-file = sing-box-tray.log
");
                }
                catch {}
            }

            var ini = new IniFile(iniPath);
            Options.SbDir = ini.ReadString("tray-config", "sb-dir", _dataDir);
            if (string.IsNullOrEmpty(Options.SbDir))
                Options.SbDir = _dataDir;
            else
                Options.SbDir = Path.GetFullPath(Options.SbDir);

            string sbConfigFile = ini.ReadString("tray-config", "sb-config-file", "config.json");
            if (!Path.IsPathRooted(sbConfigFile))
            {
                var localPath = Path.Combine(_dataDir, sbConfigFile);
                if (File.Exists(localPath))
                    Options.SbConfigFile = localPath;
                else if (File.Exists(Path.Combine(Options.SbDir, sbConfigFile)))
                    Options.SbConfigFile = Path.Combine(Options.SbDir, sbConfigFile);
                else
                    Options.SbConfigFile = localPath;
            }
            else
            {
                Options.SbConfigFile = sbConfigFile;
            }

            Options.SystemProxyAuto = ini.ReadBool("tray-config", "system-proxy-auto", false);
            Options.SystemProxyBypass = ini.ReadString("tray-config", "system-proxy-bypass", "");
            Options.SystemProxyBypassLocal = ini.ReadBool("tray-config", "system-proxy-bypass-local", false);
            Options.TunStartMode = ini.ReadString("tray-config", "tun-start-mode", "off");
            
            string logFile = ini.ReadString("tray-config", "log-file", "");
            if (!string.IsNullOrEmpty(logFile) && !Path.IsPathRooted(logFile))
                Options.LogFile = Path.Combine(_dataDir, logFile);
            else
                Options.LogFile = logFile;
        }

        private void LoadSingBoxConfig()
        {
            if (!File.Exists(Options.SbConfigFile))
                throw new FileNotFoundException($"Configuration file not found: {Options.SbConfigFile}");

            var jsonText = File.ReadAllText(Options.SbConfigFile);
            JsonNode root;
            try
            {
                root = JsonNode.Parse(jsonText) ?? throw new Exception("Invalid JSON");
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to parse config.json: {ex.Message}");
            }

            // 1. 读取 Inbounds 端口及类型
            var inbounds = root["inbounds"] as JsonArray;
            if (inbounds != null)
            {
                foreach (var inbound in inbounds)
                {
                    if (inbound == null) continue;
                    var type = inbound["type"]?.ToString();
                    if (string.Equals(type, "mixed", StringComparison.OrdinalIgnoreCase))
                    {
                        var portStr = inbound["listen_port"]?.ToString();
                        if (int.TryParse(portStr, out int port))
                        {
                            SbConfig.ProxyPort = port;
                        }
                    }
                    else if (string.Equals(type, "tun", StringComparison.OrdinalIgnoreCase))
                    {
                        SbConfig.HasTunInbound = true;
                    }
                }
            }

            if (SbConfig.ProxyPort <= 0)
            {
                throw new Exception("No suitable mixed inbound found for system proxy.");
            }

            // 3. 读取 Clash API / Dashboard 地址
            var experimental = root["experimental"];
            if (experimental != null)
            {
                var clashApi = experimental["clash_api"];
                if (clashApi != null)
                {
                    var extCtrl = clashApi["external_controller"]?.ToString();
                    if (!string.IsNullOrEmpty(extCtrl))
                    {
                        SbConfig.ExternalController = extCtrl;
                    }
                }
            }

            // 2. 生成带/不带 TUN 的 JSON
            SbConfig.JsonWithTun = root.ToJsonString();

            if (SbConfig.HasTunInbound)
            {
                // 拷贝一份对象，然后把 inbounds 数组中的 tun 节点删除
                var nodeCopy = JsonNode.Parse(jsonText)!;
                var inboundsCopy = nodeCopy["inbounds"] as JsonArray;
                if (inboundsCopy != null)
                {
                    for (int i = inboundsCopy.Count - 1; i >= 0; i--)
                    {
                        var ib = inboundsCopy[i];
                        if (ib != null && string.Equals(ib["type"]?.ToString(), "tun", StringComparison.OrdinalIgnoreCase))
                        {
                            inboundsCopy.RemoveAt(i);
                        }
                    }
                }
                SbConfig.JsonWithoutTun = nodeCopy.ToJsonString();
            }
            else
            {
                SbConfig.JsonWithoutTun = SbConfig.JsonWithTun;
            }
        }
    }
}
