using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using LeagueClient.RiotInterface.Chat;
using LeagueClient.RiotInterface.Riot;
using LeagueClient.RiotInterface.Riot.Platform;
using LegendaryClient.Logic.SWF;
using MFroehlich.League.Assets;
using MFroehlich.Parsing.DynamicJSON;
using RtmpSharp.IO;
using RtmpSharp.Messaging;
using RtmpSharp.Net;
using MyChampDTO = MFroehlich.League.DataDragon.ChampionDto;

namespace LeagueClient {
  public static class Client {
    internal const string
      AirClientParentDir = @"C:\Riot Games\League of Legends\RADS\projects\lol_air_client",
      Server = "prod.na2.lol.riotgames.com",
      LoginQueue = "https://lq.na2.lol.riotgames.com/",
      ChatServer = "chat.na2.lol.riotgames.com",
      Locale = "en_US";

    internal static readonly string
      DataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\MFro\LeagueClient\",
      SettingsFile = Path.Combine(DataPath, "settings.mfro"),
      FFMpegPath = Path.Combine(DataPath, "ffmpeg.exe"),
      LoginVideoPath = Path.Combine(DataPath, "login.mp4");

    private static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    internal static event EventHandler<MessageReceivedEventArgs> MessageReceived;

    internal static RtmpClient RtmpConn;

    internal static Session UserSession;

    internal static LoginDataPacket LoginPacket;

    internal static string AirVersion;

    internal static RiotChat ChatManager;

    internal static string AirDirectory;

    internal static string LoginTheme;

    internal static MainWindow MainWindow;

    internal static LeagueClient.ClientUI.ClientPage MainPage;

    internal static GameQueueConfig[] AvailableQueues;

    internal static List<ChampionDTO> RiotChampions;
    internal static List<MyChampDTO> AvailableChampions;

    internal static List<int> EnabledMaps;

    internal static Settings Settings;

    public static long GetMilliseconds() {
      return (long) DateTime.UtcNow.Subtract(Epoch).TotalMilliseconds;
    }

    public static void PreInitialize(MainWindow window) {
      if (!Directory.Exists(DataPath))
        Directory.CreateDirectory(DataPath);

      MainWindow = window;

      MFroehlich.League.RiotAPI.RiotAPI.UrlFormat
        = "https://na.api.pvp.net{0}&api_key=25434b55-24de-40eb-8632-f88cc02fea25";

      Settings = new Settings(SettingsFile);

      var versions = Directory.EnumerateDirectories(Path.Combine(AirClientParentDir, "releases"));
      Version newest = new Version(0, 0, 0, 0);
      foreach (var dir in versions) {
        Version parsed;
        if (Version.TryParse(Path.GetFileName(dir), out parsed) && parsed > newest) newest = parsed;
      }
      AirDirectory = Path.Combine(AirClientParentDir, "releases", newest.ToString(), "deploy");

      var reader = new SWFReader(Path.Combine(AirDirectory, "lib", "ClientLibCommon.dat"));
      foreach (var tag in reader.Tags) {
        if (tag is LegendaryClient.Logic.SWF.SWFTypes.DoABC) {
          var abcTag = (LegendaryClient.Logic.SWF.SWFTypes.DoABC) tag;
          if (abcTag.Name.Contains("riotgames/platform/gameclient/application/Version")) {
            var str = System.Text.Encoding.Default.GetString(abcTag.ABCData);
            //Ugly hack ahead - turn back now! (http://pastebin.com/yz1X4HBg)
            string[] firstSplit = str.Split((char) 6);
            string[] secondSplit = firstSplit[0].Split((char) 19);
            Client.AirVersion = secondSplit[1];
          }
        }
      }

      var theme = File.ReadAllText(Path.Combine(AirDirectory, "theme.properties"));
      LoginTheme = theme.Substring("themeConfig=", ",");

      if(!File.Exists(FFMpegPath))
      using (var ffmpeg = new FileStream(FFMpegPath, FileMode.Create))
        ffmpeg.Write(LeagueClient.Properties.Resources.ffmpeg, 0,
          LeagueClient.Properties.Resources.ffmpeg.Length);
    }

    public static async Task<bool> Initialize(string user, string pass) {
      try {
        var watch = new Stopwatch(); watch.Start();
        var context = RiotCalls.RegisterObjects();
        RtmpConn = new RtmpClient(new Uri("rtmps://" + Server + ":2099"),
          context, RtmpSharp.IO.ObjectEncoding.Amf3);
        RtmpConn.MessageReceived += RtmpConn_MessageReceived;
        RtmpConn.CallbackException += CallbackException;
        RiotCalls.OnInvocationError += CallbackException;
        await RtmpConn.ConnectAsync();

        var creds = new AuthenticationCredentials();
        creds.Username = user;
        creds.Password = pass;
        creds.ClientVersion = AirVersion;
        creds.IpAddress = await RiotCalls.GetIpAddress();
        creds.Locale = Locale;
        creds.Domain = "lolclient.lol.riotgames.com";
        creds.AuthToken = await RiotCalls.GetAuthKey(creds.Username, creds.Password, LoginQueue);
        Client.ChatManager = new RiotInterface.Chat.RiotChat(user, pass);

        UserSession = await RiotCalls.Login(creds);
        await RtmpConn.SubscribeAsync("my-rtmps", "messagingDestination",
          "bc", "bc-" + UserSession.AccountSummary.AccountId.ToString());
        await RtmpConn.SubscribeAsync("my-rtmps", "messagingDestination",
          "gn-" + UserSession.AccountSummary.AccountId.ToString(),
          "gn-" + UserSession.AccountSummary.AccountId.ToString());
        await RtmpConn.SubscribeAsync("my-rtmps", "messagingDestination",
          "cn-" + UserSession.AccountSummary.AccountId.ToString(),
          "cn-" + UserSession.AccountSummary.AccountId.ToString());

        bool authed = await RtmpConn.LoginAsync(creds.Username.ToLower(), UserSession.Token);
        LoginPacket = await RiotCalls.GetLoginDataPacketForUser();
        string state = await RiotCalls.GetAccountState();

        new System.Threading.Thread(() => {
          RiotCalls.GetAvailableQueues()
            .ContinueWith(q => AvailableQueues = q.Result);
          RiotCalls.GetAvailableChampions()
            .ContinueWith(GotChampions);
        }).Start();

        EnabledMaps = new List<int>();
        foreach (var item in LoginPacket.ClientSystemStates.gameMapEnabledDTOList)
          EnabledMaps.Add((int) item["gameMapId"]);

        if (!state.Equals("ENABLED")) TryBreak("state is not ENABLED");

        return state.Equals("ENABLED");
      } catch (Exception x) {
        TryBreak("Exception " + x.Message);
        return false;
      }
    }

    public static void JoinQueue(GameQueueConfig queue, string Bots) {
      
    }

    public static void CallbackException(object sender, Exception e) {
      throw new NotImplementedException();
    }

    public static void RtmpConn_MessageReceived(object sender, MessageReceivedEventArgs e) {
      Log("Receive [{1}, {2}]: '{0}'", e.Body, e.Subtopic, e.ClientId);
      if(MessageReceived != null) MessageReceived(sender, e);
      if (e.Body is AsObject) {
        //var content = JSON.ParseObject((string) ((AsObject) e.Body)["configs"]);
      }
    }

    public static void TryBreak(string reason) {
      Log("Attempt Break: " + reason);
      if (Debugger.IsAttached) Debugger.Break();
    }

    public static void Log(object msg) {
      Console.WriteLine(msg);
    }

    public static void Log(string msg, params object[] args) {
      if (args.Length == 0) Log((object) msg);
      else Log((object) string.Format(msg, args));
    }

    private static void GotChampions(Task<ChampionDTO[]> Champs) {
      if (Champs.IsFaulted) {
        if (System.Diagnostics.Debugger.IsAttached) System.Diagnostics.Debugger.Break();
        return;
      }
      RiotChampions = new List<ChampionDTO>(Champs.Result);
      AvailableChampions = new List<MyChampDTO>();
      foreach (var item in Champs.Result)
        AvailableChampions.Add(LeagueData.GetChampData(item.ChampionId));
    }

    public static string Substring(this string str, string prefix, string suffix) {
      int start = str.IndexOf(prefix) + prefix.Length;
      int end = str.IndexOf(suffix, start);
      return str.Substring(start, end - start);
    }
  }
}
