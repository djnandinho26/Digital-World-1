using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections;
using System.Net;

namespace Digital_World.Helpers
{
    public class Settings
    {
        public class DatabaseSettings
        {
            [JsonPropertyName("Host")]
            public string Host { get; set; } = "localhost";
            [JsonPropertyName("Username")]
            public string Username { get; set; } = "";
            [JsonPropertyName("Password")]
            public string Password { get; set; } = "";
            [JsonPropertyName("Schema")]
            public string Schema { get; set; } = "";
        }

        public class ServerSettings
        {
            public string Host { get; set; } = "0.0.0.0";
            public int Port { get; set; } = 7030;
            public bool AutoStart { get; set; } = false;

            [JsonIgnore]
            public IPEndPoint EndPoint
            {
                get
                {
                    IPAddress IP = Dns.GetHostEntry(Host).AddressList[0];
                    IPEndPoint ipep = new IPEndPoint(IP, Port);
                    return ipep;
                }
            }

            [JsonIgnore]
            public IPAddress IP
            {
                get
                {
                    IPAddress? myIp = null;
                    if (!IPAddress.TryParse(Host, out myIp))
                    {
                        IPAddress[] List = Dns.GetHostEntry(Host).AddressList;
                        foreach (IPAddress ip in List)
                        {
                            if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                            {
                                myIp = ip;
                                break;
                            }
                        }
                        if (myIp == null)
                            myIp = List[0];
                    }
                    return myIp;
                }
            }
        }

        public class AuthServerSettings:ServerSettings
        {
            [JsonPropertyName("HttpPort")]
            public int HttpPort { get; set; } = 8080;
            
            [JsonPropertyName("HttpsPort")]
            public int HttpsPort { get; set; } = 8443;
            
            [JsonPropertyName("PatchPath")]
            public string PatchPath { get; set; } = "C:\\DMOServer\\HttpServer\\www";
            
            [JsonPropertyName("HttpEnabled")]
            public bool HttpEnabled { get; set; } = true;
            
            [JsonPropertyName("HttpsEnabled")]
            public bool HttpsEnabled { get; set; } = false;
            
            [JsonPropertyName("CertificatePath")]
            public string CertificatePath { get; set; } = "C:\\DMOServer\\HttpServer\\cert\\certificate.pfx";
            
            [JsonPropertyName("CertificatePassword")]
            public string CertificatePassword { get; set; } = "";
            
            [JsonPropertyName("CertificateType")]
            public string CertificateType { get; set; } = "Auto"; // "Auto" ou "ZeroSSL"

            public AuthServerSettings()
            {
                this.AutoStart = true;
                this.Port = 7029;
            }
        }

        public class LobbyServerSettings : ServerSettings
        {
            public LobbyServerSettings()
            {
                this.AutoStart = true;
                this.Port = 7030;
            }
        }

        public class GameServerSettings : ServerSettings
        {
            public HatchRateSetting HatchRates { get; set; } = new HatchRateSetting();

            public class HatchRateSetting
            {
                [JsonIgnore]
                private double[][] HatchRate = new double[5][] { new double[3], new double[3], new double[3], new double[3], new double[3] };

                public HatchRateSetting()
                {
                    HatchRate[0] = new double[] { 0.90d, 0.10d, 0.0d };
                    HatchRate[1] = new double[] { 0.80d, 0.15d, 0.05d };
                    HatchRate[2] = new double[] { 0.70d, 0.25d, 0.05d };
                    HatchRate[3] = new double[] { 0.50d, 0.35d, 0.15d };
                    HatchRate[4] = new double[] { 0.30d, 0.50d, 0.20d };
                }

                public class HatchLevelSetting
                {
                    [JsonPropertyName("Success")]
                    public double Success { get; set; } = 1.0d;
                    [JsonPropertyName("Failure")]
                    public double Failure { get; set; } = 0.0d;
                    [JsonPropertyName("Broken")]
                    public double Broken { get; set; } = 0.0d;

                    public HatchLevelSetting(double success, double failure, double broken)
                    {
                        Success = success;
                        Failure = failure;
                        Broken = broken;
                    }

                    public HatchLevelSetting() { }

                    public HatchLevelSetting(double[] p)
                    {
                        this.Success = p[0];
                        this.Failure = p[1];
                        this.Broken = p[2];
                    }

                    public double[] ToArray()
                    {
                        return new double[] { Success, Failure, Broken };
                    }

                    public List<Tuple<int, double>> ToList()
                    {
                        List<Tuple<int, double>> list = new List<Tuple<int, double>>();
                        list.Add(new Tuple<int, double>(0, Success));
                        list.Add(new Tuple<int, double>(1, Failure));
                        list.Add(new Tuple<int, double>(2, Broken));
                        //list.Sort((a, b) => a.Item2.CompareTo(b.Item2));
                        return list;
                    }

                    public double Sum()
                    {
                        return Success + Failure + Broken;
                    }
                }

                public HatchLevelSetting Level1
                {
                    get
                    {
                        return new HatchLevelSetting(HatchRate[0]);
                    }
                    set
                    {
                        HatchRate[0] = value.ToArray();
                    }
                }

                public HatchLevelSetting Level2
                {
                    get
                    {
                        return new HatchLevelSetting(HatchRate[1]);
                    }
                    set
                    {
                        HatchRate[1] = value.ToArray();
                    }
                }

                public HatchLevelSetting Level3
                {
                    get
                    {
                        return new HatchLevelSetting(HatchRate[2]);
                    }
                    set
                    {
                        HatchRate[2] = value.ToArray();
                    }
                }

                public HatchLevelSetting Level4
                {
                    get
                    {
                        return new HatchLevelSetting(HatchRate[3]);
                    }
                    set
                    {
                        HatchRate[3] = value.ToArray();
                    }
                }

                public HatchLevelSetting Level5
                {
                    get
                    {
                        return new HatchLevelSetting(HatchRate[4]);
                    }
                    set
                    {
                        HatchRate[4] = value.ToArray();
                    }
                }

                [JsonIgnore]
                private Random RNG = new Random();

                public int Hatch(int level)
                {
                    int result = 0;
                    HatchLevelSetting hls = new HatchLevelSetting(HatchRate[level]);
                    List<Tuple<int, double>> list = hls.ToList();
                    double rnd = RNG.NextDouble();

                    foreach(Tuple<int, double> rate in list)
                    {
                        if (rnd < rate.Item2)
                        {
                            result = rate.Item1;
                            break;
                        }
                        rnd -= rate.Item2;
                    }
                    return result;
                }
            }

            public class SizeSetting
            {
                [JsonPropertyName("min")]
                public int Min { get; set; } = 0;
                [JsonPropertyName("max")]
                public int Max { get; set; } = 0;

                public SizeSetting() { }

                public SizeSetting(int i1, int i2)
                {
                    Min = i1;
                    Max = i2;
                }

                public int Size(Random RNG)
                {
                        if (Min > Max)
                            Min = Max - 1;
                        if (Max < Min)
                            Max = Min + 1;
                        return RNG.Next(Min * 100, Max * 100);

                }
            }

            public class SizeSettingContainer
            {
                public SizeSetting Level3 = new SizeSetting(70, 100);
                public SizeSetting Level4 = new SizeSetting(100, 130);
                public SizeSetting Level5 = new SizeSetting(130, 160);
                private Random RNG = new Random();

                public SizeSettingContainer() { }

                public int Size(int Level)
                {
                    if (Level == 3)
                        return Level3.Size(RNG);
                    if (Level == 4)
                        return Level4.Size(RNG);
                    if (Level == 5)
                        return Level5.Size(RNG);
                    return 65000;
                }
            }

            public SizeSettingContainer SizeRanges { get; set; } = new SizeSettingContainer();

            public GameServerSettings()
            {
                this.Port = 7000;
            }
        }

        public Settings()
        {
            Database = new DatabaseSettings();
            LobbyServer = new LobbyServerSettings();
            AuthServer = new AuthServerSettings();
            GameServer = new GameServerSettings();
        }

        [JsonPropertyName("Database")]
        public DatabaseSettings Database { get; set; } = new DatabaseSettings();
        [JsonPropertyName("LobbyServer")]
        public LobbyServerSettings LobbyServer { get; set; } = new LobbyServerSettings();
        [JsonPropertyName("AuthServer")]
        public AuthServerSettings AuthServer { get; set; } = new AuthServerSettings();
        [JsonPropertyName("GameServer")]
        public GameServerSettings GameServer { get; set; } = new GameServerSettings();

        public void Serialize(string fileName)
        {
            var options = new JsonSerializerOptions 
            { 
                WriteIndented = true,
                PropertyNamingPolicy = null
            };
            string json = JsonSerializer.Serialize(this, options);
            File.WriteAllText(fileName, json);
        }

        public static Settings Deserialize(string fileName)
        {
            Settings settings = new Settings();
            
            // Try JSON first
            string jsonFileName = Path.ChangeExtension(fileName, ".json");
            if (File.Exists(jsonFileName))
            {
                try
                {
                    string json = File.ReadAllText(jsonFileName);
                    var options = new JsonSerializerOptions { PropertyNamingPolicy = null };
                    settings = JsonSerializer.Deserialize<Settings>(json, options) ?? new Settings();
                }
                catch
                {
                    // Se falhar ao ler JSON, cria um novo com valores padrão
                    settings = new Settings();
                    settings.Serialize(jsonFileName);
                }
            }
            // Fallback to XML and auto-migrate
            else if (File.Exists(fileName))
            {
                try
                {
                    var xml = new System.Xml.Serialization.XmlSerializer(typeof(Settings));
                    using (Stream s = File.OpenRead(fileName))
                    {
                        settings = (Settings?)xml.Deserialize(s) ?? new Settings();
                    }
                    // Auto-migrate to JSON
                    settings.Serialize(jsonFileName);
                    try { File.Delete(fileName); } catch { }
                }
                catch
                {
                    // Se falhar ao migrar, cria JSON novo
                    settings = new Settings();
                    settings.Serialize(jsonFileName);
                }
            }
            else
            {
                // Arquivo não existe - cria JSON com valores padrão
                settings.Serialize(jsonFileName);
            }
            
            SqlDB.SetInfo(settings.Database.Host, settings.Database.Username, settings.Database.Password, settings.Database.Schema);
            return settings;
        }

        public static Settings Deserialize()
        {
            return Deserialize("Settings.json");
        }

        public void Serialize()
        {
            Serialize("Settings.json");
        }
    }
}