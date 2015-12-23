using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.Xml;
using LeagueClient.ClientUI.Controls;
using LeagueClient.ClientUI.Main;
using LeagueClient.Logic;
using LeagueClient.Logic.Chat;
using LeagueClient.Logic.com.riotgames.other;
using LeagueClient.Logic.Queueing;
using LeagueClient.Logic.Riot;
using LeagueClient.Logic.Riot.Platform;
using LeagueClient.Logic.Riot.Team;
using MFroehlich.League;
using MFroehlich.League.Assets;
using MFroehlich.League.RiotAPI;
using MFroehlich.Parsing.JSON;
using RtmpSharp.IO;
using RtmpSharp.Messaging;
using RtmpSharp.Net;
using MyChampDTO = MFroehlich.League.DataDragon.ChampionDto;
using LeagueClient.Logic.Settings;
using System.Xml.Serialization;
using System.Net;
using System.Text;
using System.Windows;

namespace LeagueClient.Logic {
  public static class Client {
    internal static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    #region Constants
    internal static readonly Region Region = Region.NA;

    internal static readonly string
      RiotGamesDir = @"D:\Riot Games\" + (Region == Region.PBE ? "PBE" : "League of Legends"),
      Locale = "en_US",
      DataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\MFro\LeagueClient\",
      SettingsFile = Path.Combine(DataPath, "settings.xml"),
      FFMpegPath = Path.Combine(DataPath, "ffmpeg.exe"),
      LoginVideoPath = Path.Combine(DataPath, "login.mp4"),
      LoginStaticPath = Path.Combine(DataPath, "back.png"),
      LogFilePath = Path.Combine(DataPath, "log.txt");

    internal static Strings Strings { get; } = Strings.en_US;
    #endregion

    #region Properties

    internal static RtmpClient RtmpConn { get; set; }

    internal static Session UserSession { get; set; }

    internal static LoginQueueDto LoginQueue { get; set; }
    internal static LoginDataPacket LoginPacket { get; set; }
    internal static string ReconnectToken { get; set; }
    internal static bool Connected { get; set; }

    internal static RiotVersionManager Latest { get; set; }
    internal static RiotVersionManager Installed { get; set; }

    internal static string LoginTheme { get; set; }

    internal static MainWindow MainWindow { get; set; }
    internal static PopupSelector PopupSelector { get; set; }

    internal static RiotChat ChatManager { get; set; }
    internal static IQueueManager QueueManager { get; set; }
    internal static SummonerCache SummonerCache { get; set; }

    internal static Dictionary<int, GameQueueConfig> AvailableQueues { get; set; }

    internal static List<ChampionDTO> RiotChampions { get; set; }
    internal static List<MyChampDTO> AvailableChampions { get; set; }

    internal static SpellBookDTO Runes { get; private set; }
    internal static MasteryBookDTO Masteries { get; private set; }
    internal static SpellBookPageDTO SelectedRunePage { get; private set; }
    internal static MasteryBookPageDTO SelectedMasteryPage { get; private set; }

    internal static PlayerDTO RankedTeamInfo { get; private set; }

    internal static List<int> EnabledMaps { get; set; }

    internal static UserSettings Settings { get; set; }

    internal static dynamic Credentials { get; set; }
    internal static AsyncProperty<RiotAPI.CurrentGameAPI.CurrentGameInfo> CurrentGame { get; set; }

    internal static bool CanInviteFriends { get; set; }
    #endregion 

    #region Initailization

    public static async Task PreInitialize(MainWindow window) {
      if (!Directory.Exists(DataPath))
        Directory.CreateDirectory(DataPath);
      Console.SetOut(TextWriter.Null);
      Console.SetError(TextWriter.Null);

      MainWindow = window;

      RiotAPI.UrlFormat = "https://na.api.pvp.net{0}&api_key=25434b55-24de-40eb-8632-f88cc02fea25";

      Installed = RiotVersionManager.FetchInstalled(Region, RiotGamesDir);
      Latest = await RiotVersionManager.FetchLatest(Region);
      using (var web = new WebClient()) {
        var theme = Latest.AirFiles.FirstOrDefault(f => f.Url.AbsolutePath.EndsWith("/files/theme.properties"));
        var content = web.DownloadString(theme.Url);
        LoginTheme = content.Substring("themeConfig=", ",");
      }

      if (!File.Exists(FFMpegPath))
        using (var ffmpeg = new FileStream(FFMpegPath, FileMode.Create))
          ffmpeg.Write(LeagueClient.Properties.Resources.ffmpeg, 0, LeagueClient.Properties.Resources.ffmpeg.Length);
    }

    public static async Task<bool> Initialize(string user, string pass) {
      LoginQueue = await RiotServices.GetAuthKey(user, pass);
      if (LoginQueue.Token == null) return false;

      var context = RiotServices.RegisterObjects();
      RtmpConn = new RtmpClient(new Uri("rtmps://" + Region.MainServer + ":2099"), context, RtmpSharp.IO.ObjectEncoding.Amf3);
      RtmpConn.MessageReceived += RtmpConn_MessageReceived;
      RtmpConn.Disconnected += RtmpConn_Disconnected;
      await RtmpConn.ConnectAsync();

      var creds = new AuthenticationCredentials();
      creds.Username = user;
      creds.Password = pass;
      creds.ClientVersion = LeagueData.CurrentVersion;
      creds.Locale = Locale;
      creds.Domain = "lolclient.lol.riotgames.com";
      creds.AuthToken = LoginQueue.Token;
      UserSession = await RiotServices.LoginService.Login(creds);

      var bc = $"bc-{UserSession.AccountSummary.AccountId}";
      var gn = $"gn-{UserSession.AccountSummary.AccountId}";
      var cn = $"cn-{UserSession.AccountSummary.AccountId}";
      var tasks = new[] {
        RtmpConn.SubscribeAsync("my-rtmps", "messagingDestination", "bc", bc),
        RtmpConn.SubscribeAsync("my-rtmps", "messagingDestination", gn, gn),
        RtmpConn.SubscribeAsync("my-rtmps", "messagingDestination", cn, cn),
      };
      await Task.WhenAll(tasks);

      bool authed = await RtmpConn.LoginAsync(creds.Username.ToLower(), UserSession.Token);
      string state = await RiotServices.AccountService.GetAccountState();
      LoginPacket = await RiotServices.ClientFacadeService.GetLoginDataPacketForUser();
      Connected = true;
      ReconnectToken = Convert.ToBase64String(Encoding.UTF8.GetBytes(UserSession.AccountSummary.Username + ":" + LoginQueue.Token));

      StartHeartbeat();
      new Thread(() => {
        RiotServices.MatchmakerService.GetAvailableQueues().ContinueWith(GotQueues);
        RiotServices.InventoryService.GetAvailableChampions().ContinueWith(GotChampions);
        RiotServices.SummonerTeamService.CreatePlayer().ContinueWith(GotRankedTeamInfo);

        Runes = LoginPacket.AllSummonerData.SpellBook;
        Masteries = LoginPacket.AllSummonerData.MasteryBook;
        SelectedRunePage = Runes.BookPages.FirstOrDefault(p => p.Current);
        SelectedMasteryPage = Masteries.BookPages.FirstOrDefault(p => p.Current);

        RiotServices.GameInvitationService.GetPendingInvitations().ContinueWith(t => {
          foreach (var invite in t.Result) {
            if (invite is InvitationRequest)
              ShowInvite((InvitationRequest) invite);
          }
        });
      }).Start();

      EnabledMaps = new List<int>();
      foreach (var item in LoginPacket.ClientSystemStates.gameMapEnabledDTOList)
        EnabledMaps.Add((int) item["gameMapId"]);

      if (state?.Equals("ENABLED") != true) Console.WriteLine(state);

      Settings.ProfileIcon = LoginPacket.AllSummonerData.Summoner.ProfileIconId;
      Settings.SummonerName = LoginPacket.AllSummonerData.Summoner.Name;
      SummonerCache = new SummonerCache();

      return state.Equals("ENABLED");
    }

    private static System.Timers.Timer HeartbeatTimer;
    private static int Heartbeats;

    private static void StartHeartbeat() {
      HeartbeatTimer = new System.Timers.Timer();
      HeartbeatTimer.Elapsed += HeartbeatTimer_Elapsed;
      HeartbeatTimer.Interval = 120000;
      HeartbeatTimer.Start();
      HeartbeatTimer_Elapsed(HeartbeatTimer, null);
    }

    private static async void HeartbeatTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e) {
      if (Connected) {
        var result = await RiotServices.LoginService.PerformLCDSHeartBeat((int) LoginPacket.AllSummonerData.Summoner.AccountId,
          UserSession.Token, Heartbeats, DateTime.Now.ToString("ddd MMM d yyyy HH:mm:ss 'GMT-0700'"));
        if (!result.Equals("5")) {
          Console.WriteLine("Heartbeat unexpected");
        }
        Heartbeats++;
      }
    }

    private static void GotChampions(Task<ChampionDTO[]> Champs) {
      if (Champs.IsFaulted) {
        if (Debugger.IsAttached) System.Diagnostics.Debugger.Break();
        return;
      }
      RiotChampions = new List<ChampionDTO>(Champs.Result);
      AvailableChampions = new List<MyChampDTO>();
      foreach (var item in Champs.Result)
        AvailableChampions.Add(LeagueData.GetChampData(item.ChampionId));
    }

    private static void GotQueues(Task<GameQueueConfig[]> Task) {
      AvailableQueues = new Dictionary<int, GameQueueConfig>();
      foreach (var item in Task.Result)
        if (LoginPacket.AllSummonerData.SummonerLevel.Level >= item.MinLevel && LoginPacket.AllSummonerData.SummonerLevel.Level <= item.MaxLevel)
          AvailableQueues.Add(item.Id, item);
    }

    private static void GotRankedTeamInfo(Task<PlayerDTO> obj) {
      RankedTeamInfo = obj.Result;
    }

    #endregion

    #region Riot Client Methods
    public static void JoinGame() {
      //"8394" "LoLPatcher.exe" "" "ip port key id"
      if (Process.GetProcessesByName("League of Legends").Length > 0) {
        ChatManager.Status = ChatStatus.inGame;
        Application.Current.Dispatcher.Invoke(() => QueueManager.ShowPage(new InGamePage()));
        new Thread(GetCurrentGame).Start();
        return;
      }

      var game = Path.Combine(RiotGamesDir, RiotVersionManager.SolutionPath, Latest.SolutionVersion.ToString(), "deploy");
      var lolclient = Path.Combine(RiotGamesDir, RiotVersionManager.AirPath, Latest.AirVersion.ToString(), "deploy", "LolClient.exe");

      var info = new ProcessStartInfo(Path.Combine(game, "League of Legends.exe"));
      var str = $"{Credentials.ServerIp} {Credentials.ServerPort} {Credentials.EncryptionKey} {Credentials.SummonerId}";
      info.Arguments = string.Format("\"{0}\" \"{1}\" \"{2}\" \"{3}\"", "8394", "LoLPatcher.exe", lolclient, str);
      info.WorkingDirectory = game;
      Process.Start(info);

      ChatManager.Status = ChatStatus.inGame;
      Application.Current.Dispatcher.Invoke(() => QueueManager.ShowPage(new InGamePage()));
      new Thread(GetCurrentGame).Start();
    }

    public static void ShowInvite(InvitationRequest invite) {
      if (invite.InvitationState.Equals("ACTIVE")) {
        var user = RiotChat.GetUser(invite.Inviter.summonerId);
        if (ChatManager.Friends.ContainsKey(user)) {
          ChatManager.Friends[user].Invite = invite;
        } else {

        }
      }
    }

    public static void Logout() {
      if (Connected) {
        try {
          SaveSettings(Settings.Username, Settings);
          RtmpConn.MessageReceived -= RtmpConn_MessageReceived;
          HeartbeatTimer.Dispose();
          new Thread(async () => {
            Connected = false;
            await RiotServices.GameService.QuitGame();
            await RiotServices.GameInvitationService.Leave();
            await RiotServices.LoginService.Logout();
            await RtmpConn.LogoutAsync();
            RtmpConn.Close();
          }).Start();
        } catch { }
      }
      ChatManager?.Logout();
      MainWindow.Start();
    }
    #endregion

    #region Runes and Masteries

    /// <summary>
    /// Selects a mastery page as the default selected page for your account and
    /// updates the contents of the local and server-side mastery books
    /// </summary>
    /// <param name="page">The page to select</param>
    public static async void SelectMasteryPage(MasteryBookPageDTO page) {
      if (page == SelectedMasteryPage) return;
      foreach (var item in Masteries.BookPages) item.Current = false;
      page.Current = true;
      SelectedMasteryPage = page;
      await RiotServices.MasteryBookService.SelectDefaultMasteryBookPage(page);
      await RiotServices.MasteryBookService.SaveMasteryBook(Masteries);
    }

    /// <summary>
    /// Selects a rune page as the default selected page for your account and
    /// updates the contents of the local and server-side spell books
    /// </summary>
    /// <param name="page">The page to select</param>
    public static async void SelectRunePage(SpellBookPageDTO page) {
      if (page == SelectedRunePage) return;
      foreach (var item in Runes.BookPages) item.Current = false;
      page.Current = true;
      SelectedRunePage = page;
      await RiotServices.SpellBookService.SelectDefaultSpellBookPage(page);
      await RiotServices.SpellBookService.SaveSpellBook(Runes);
    }

    /// <summary>
    /// Deletes a mastery page from your mastery page book and updates the
    /// contents of the local and server-side mastery books
    /// </summary>
    /// <param name="page">The page to delete</param>
    public static void DeleteMasteryPage(MasteryBookPageDTO page) {
      if (!Masteries.BookPages.Contains(page)) throw new ArgumentException("Book page not found: " + page);
      Masteries.BookPages.Remove(page);
      SelectedMasteryPage = Masteries.BookPages.First();
      SelectedMasteryPage.Current = true;
      RiotServices.MasteryBookService.SaveMasteryBook(Masteries);
    }

    #endregion

    #region My Client Methods

    public static void ShowPopup(PopupSelector.Selector thing) {
      PopupSelector.CurrentSelector = thing;
      PopupSelector.BeginStoryboard(App.FadeIn);
    }

    public static void HidePopup() {
      PopupSelector.BeginStoryboard(App.FadeOut);
    }

    private static void GetCurrentGame() {
      Thread.Sleep(20000);
      CurrentGame = new Task<RiotAPI.CurrentGameAPI.CurrentGameInfo>(() => {
        try {
          return RiotAPI.CurrentGameAPI.BySummoner("NA1", LoginPacket.AllSummonerData.Summoner.SummonerId);
        } catch (Exception x) {
          Log("Failed to get game data: " + x);
          return null;
        }
      });
    }

    public static T LoadSettings<T>(string name) where T : ISettings, new() {
      name = name.RemoveAllWhitespace();
      var file = Path.Combine(DataPath, name + ".settings");
      if (File.Exists(file)) {
        using (var stream = new FileStream(file, FileMode.Open)) {
          var xml = new XmlSerializer(typeof(T));
          return (T) xml.Deserialize(stream);
        }
      } else return new T();
    }

    public static void SaveSettings<T>(string name, T settings) where T : ISettings {
      name = name.RemoveAllWhitespace();
      using (var stream = new FileStream(Path.Combine(DataPath, name + ".settings"), FileMode.Create)) {
        var xml = new XmlSerializer(typeof(T));
        xml.Serialize(stream, settings);
      }
    }

    private static object _lock = new object();
    private static TextWriter LogDebug = Console.Out;
    public static void Log(object msg) {
      lock (_lock) {
        try {
          using (var log = new StreamWriter(File.Open(LogFilePath, FileMode.Append))) {
            LogDebug.WriteLine(msg);
            log.WriteLine(msg);
          }
        } catch { }
      }
    }

    public static long GetMilliseconds() => (long) DateTime.UtcNow.Subtract(Epoch).TotalMilliseconds;

    #endregion

    public static void ThrowException(Exception x, string details = null) {
      if (details != null) Log(details);
      Log(x);
    }

    public static void RtmpConn_MessageReceived(object sender, MessageReceivedEventArgs e) {
      try {
        if (MainWindow.HandleMessage(e)) return;
      } catch (Exception x) {
        ThrowException(x, "Exception while dispatching message");
      }

      var response = e.Body as LcdsServiceProxyResponse;
      var config = e.Body as ClientDynamicConfigurationNotification;
      var invite = e.Body as InvitationRequest;
      var endofgame = e.Body as EndOfGameStats;

      try {
        if (response != null) {
          if (response.status.Equals("ACK"))
            Log($"Acknowledged call of method {response.methodName} [{response.messageId}]");
          else if (response.messageId != null && RiotServices.Delegates.ContainsKey(response.messageId)) {
            RiotServices.Delegates[response.messageId](response);
            RiotServices.Delegates.Remove(response.messageId);
          } else {
            Log($"Unhandled LCDS response of method {response.methodName} [{response.messageId}], {response.payload}");
          }
        } else if (config != null) {
          Log("Received Configuration Notification");
        } else if (invite != null) {
          ShowInvite(invite);
        } else if (endofgame != null) {
          Debugger.Break();
        } else {
          Log($"Receive [{e.Subtopic}, {e.ClientId}]: '{e.Body}'");
        }
      } catch (Exception x) {
        ThrowException(x, "Exception while handling message");
      }
    }

    private static async void RtmpConn_Disconnected(object sender, EventArgs e) {
      Connected = false;
      await RtmpConn.RecreateConnection(ReconnectToken);

      var bc = $"bc-{UserSession.AccountSummary.AccountId}";
      var gn = $"gn-{UserSession.AccountSummary.AccountId}";
      var cn = $"cn-{UserSession.AccountSummary.AccountId}";
      var tasks = new[] {
        RtmpConn.SubscribeAsync("my-rtmps", "messagingDestination", "bc", bc),
        RtmpConn.SubscribeAsync("my-rtmps", "messagingDestination", gn, gn),
        RtmpConn.SubscribeAsync("my-rtmps", "messagingDestination", cn, cn),
      };
      await Task.WhenAll(tasks);
      Connected = true;
    }
  }
}
