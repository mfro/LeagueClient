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
using MFroehlich.Parsing.DynamicJSON;
using RtmpSharp.IO;
using RtmpSharp.Messaging;
using RtmpSharp.Net;
using MyChampDTO = MFroehlich.League.DataDragon.ChampionDto;
using LeagueClient.Logic.Settings;
using System.Xml.Serialization;
using System.Net;

namespace LeagueClient.Logic {
  public static class Client {
    private static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    #region Constants
    internal static readonly Region Region = Region.NA;

    internal static readonly string
      RiotGamesDir = @"D:\Riot Games\" + (Region == Region.PBE ? "PBE" : "League of Legends"),
      Locale = "en_US",
      DataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\MFro\LeagueClient\",
      SettingsFile = Path.Combine(DataPath, "settings.xml"),
      FFMpegPath = Path.Combine(DataPath, "ffmpeg.exe"),
      LoginVideoPath = Path.Combine(DataPath, "login.mp4"),
      LogFilePath = Path.Combine(DataPath, "log.txt");
    #endregion

    #region Properties
    internal static RtmpClient RtmpConn { get; set; }

    internal static Session UserSession { get; set; }

    internal static LoginDataPacket LoginPacket { get; set; }
    internal static bool Connected { get; set; }

    internal static RiotVersionManager Latest { get; set; }
    internal static RiotVersionManager Installed { get; set; }

    internal static string LoginTheme { get; set; }

    internal static MainWindow MainWindow { get; set; }

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

    internal static Process GameProcess { get; set; }
    internal static InGameCredentials QueuedCredentials { get; set; }
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

      Installed = RiotVersionManager.FetchInstalled(RiotGamesDir);
      Latest = await RiotVersionManager.FetchLatest(Region);
      using (var web = new WebClient()) {
        var url = Path.Combine(Region.UpdateBase, $"projects/lol_air_client/releases/{Latest.AirVersion}/files/theme.properties");
        var content = web.DownloadString(url);
        LoginTheme = content.Substring("themeConfig=", ",");
      }

      if (!File.Exists(FFMpegPath))
        using (var ffmpeg = new FileStream(FFMpegPath, FileMode.Create))
          ffmpeg.Write(LeagueClient.Properties.Resources.ffmpeg, 0, LeagueClient.Properties.Resources.ffmpeg.Length);
    }

    public static async Task<bool> Initialize(string user, string pass) {
      var context = RiotServices.RegisterObjects();
      RtmpConn = new RtmpClient(new Uri("rtmps://" + Region.MainServer + ":2099"), context, RtmpSharp.IO.ObjectEncoding.Amf3);
      RtmpConn.MessageReceived += RtmpConn_MessageReceived;
      await RtmpConn.ConnectAsync();

      var creds = new AuthenticationCredentials();
      creds.Username = user;
      creds.Password = pass;
      creds.ClientVersion = LeagueData.CurrentVersion;
      creds.Locale = Locale;
      creds.Domain = "lolclient.lol.riotgames.com";
      var queue = await RiotServices.GetAuthKey(creds.Username, creds.Password);
      creds.AuthToken = queue.Token;

      UserSession = await RiotServices.LoginService.Login(creds);
      await RtmpConn.SubscribeAsync("my-rtmps", "messagingDestination",
        "bc", "bc-" + UserSession.AccountSummary.AccountId.ToString());
      await RtmpConn.SubscribeAsync("my-rtmps", "messagingDestination",
        "gn-" + UserSession.AccountSummary.AccountId.ToString(),
        "gn-" + UserSession.AccountSummary.AccountId.ToString());
      await RtmpConn.SubscribeAsync("my-rtmps", "messagingDestination",
        "cn-" + UserSession.AccountSummary.AccountId.ToString(),
        "cn-" + UserSession.AccountSummary.AccountId.ToString());

      bool authed = await RtmpConn.LoginAsync(creds.Username.ToLower(), UserSession.Token);
      LoginPacket = await RiotServices.ClientFacadeService.GetLoginDataPacketForUser();
      string state = await RiotServices.AccountService.GetAccountState();
      Connected = true;

      new Thread(() => {
        RiotServices.MatchmakerService.GetAvailableQueues().ContinueWith(GotQueues);
        RiotServices.InventoryService.GetAvailableChampions().ContinueWith(GotChampions);
        RiotServices.SummonerTeamService.CreatePlayer().ContinueWith(GotRankedTeamInfo);

        Runes = LoginPacket.AllSummonerData.SpellBook;
        Masteries = LoginPacket.AllSummonerData.MasteryBook;
        SelectedRunePage = Runes.BookPages.FirstOrDefault(p => p.Current);
        SelectedMasteryPage = Masteries.BookPages.FirstOrDefault(p => p.Current);
      }).Start();

      EnabledMaps = new List<int>();
      foreach (var item in LoginPacket.ClientSystemStates.gameMapEnabledDTOList)
        EnabledMaps.Add((int) item["gameMapId"]);

      if (state?.Equals("ENABLED") != true) Console.WriteLine(state);

      Settings.ProfileIcon = LoginPacket.AllSummonerData.Summoner.ProfileIconId;
      Settings.SummonerName = LoginPacket.AllSummonerData.Summoner.Name;
      SummonerCache = new SummonerCache();

      if (queue.InGameCredentials.InGame)
        QueuedCredentials = queue.InGameCredentials;

      return state.Equals("ENABLED");
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
    /// <summary>
    /// Launches the league of legends client and joins an active game
    /// </summary>
    /// <param name="creds">The credentials for joining the game</param>
    public static void JoinGame(PlayerCredentialsDto creds) => JoinGame(creds.ServerIp, creds.ServerPort, creds.EncryptionKey, creds.SummonerId);
    /// <summary>
    /// Launches the league of legends client and joins an active game
    /// </summary>
    /// <param name="creds">The credentials for joining the game</param>
    public static void JoinGame(InGameCredentials creds) => JoinGame(creds.ServerIp, creds.ServerPort, creds.EncryptionKey, creds.SummonerId);

    private static void JoinGame(string ip, int port, string encKey, double summId) {
      //"8394" "LoLPatcher.exe" "" "ip port key id"
      var game = Path.Combine(RiotGamesDir, RiotVersionManager.SolutionPath, Latest.SolutionVersion.ToString(), "deploy");
      var lolclient = Path.Combine(RiotGamesDir, RiotVersionManager.AirPath, Latest.AirVersion.ToString(), "deploy", "LolClient.exe");

      var info = new ProcessStartInfo(Path.Combine(game, "League of Legends.exe"));
      var str = $"{ip} {port} {encKey} {summId}";
      info.Arguments = string.Format("\"{0}\" \"{1}\" \"{2}\" \"{3}\"", "8394", "LoLPatcher.exe", lolclient, str);
      info.WorkingDirectory = game;
      GameProcess = Process.Start(info);

      System.Windows.Application.Current.Dispatcher.Invoke(MainWindow.ShowInGamePage);
      ChatManager.Status = ChatStatus.inGame;
      new Thread(() => {
        Thread.Sleep(20000);
        CurrentGame = new Task<RiotAPI.CurrentGameAPI.CurrentGameInfo>(() => RiotAPI.CurrentGameAPI.BySummoner("NA", LoginPacket.AllSummonerData.Summoner.SumId));
      }).Start();
    }

    private static Dictionary<string, Alert> invites = new Dictionary<string, Alert>();
    public static void ShowInvite(InvitationRequest invite) {
      if (invite.InvitationState.Equals("ACTIVE")) {
        //var payload = JSON.ParseObject(invite.GameMetaData);
        //string type = payload["gameType"];
        //GameInviteAlert alert = AlertFactory.InviteAlert(invite);

        //invites[invite.InvitationId] = alert;
        //QueueManager.ShowNotification(alert);
        var user = RiotChat.GetUser(invite.Inviter.summonerId);
        if (ChatManager.Friends.ContainsKey(user)) {
          ChatManager.Friends[user].Invite = invite;
        } else {

        }
      }
    }

    public static async void Logout() {
      if (Connected) {
        try {
          SaveSettings(Settings.Username, Settings);
          await RiotServices.GameService.QuitGame();
          await RiotServices.LoginService.Logout();
          await RtmpConn.LogoutAsync();
          RtmpConn.Close();
          Connected = false;
        } catch { }
      }
      ChatManager?.Logout();
      MainWindow.PatchComplete();
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

    public static void RtmpConn_MessageReceived(object sender, MessageReceivedEventArgs e) {
      try {
        if (MainWindow.HandleMessage(e)) return;
      } catch (Exception x) {
        Log("Exception while dispatching message: " + x.Message);
      }

      var response = e.Body as LcdsServiceProxyResponse;
      var config = e.Body as ClientDynamicConfigurationNotification;
      var invite = e.Body as InvitationRequest;

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
        } else {
          Log($"Receive [{e.Subtopic}, {e.ClientId}]: '{e.Body}'");
        }
      } catch (Exception x) {
        Log("Exception while handling message: " + x.Message);
      }
    }
  }
}
