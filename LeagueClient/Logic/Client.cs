using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using LeagueClient.Logic.Chat;
using LeagueClient.Logic.Queueing;
using LeagueClient.Logic.Riot;
using LeagueClient.Logic.Riot.Platform;
using LegendaryClient.Logic.SWF;
using MFroehlich.League.Assets;
using RtmpSharp.IO;
using RtmpSharp.Messaging;
using RtmpSharp.Net;
using MyChampDTO = MFroehlich.League.DataDragon.ChampionDto;
using MFroehlich.Parsing.DynamicJSON;
using LeagueClient.Logic;
using LeagueClient.ClientUI.Main;
using System.Threading;
using System.Windows.Threading;
using System.Xml;

namespace LeagueClient.Logic {
  public static class Client {
    private static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    #region Constants
    internal const string
      RiotGamesDir = @"D:\Riot Games",
      Server = "prod.na2.lol.riotgames.com",
      LoginQueue = "https://lq.na2.lol.riotgames.com/",
      ChatServer = "chat.na2.lol.riotgames.com",
      Locale = "en_US";

    internal static readonly string
      AirClientParentDir = Path.Combine(RiotGamesDir, @"League of Legends\RADS\projects\lol_air_client"),
      GameClientParentDir = Path.Combine(RiotGamesDir, @"League of Legends\RADS\projects\lol_game_client"),
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

    internal static string AirVersion { get; set; }

    internal static RiotChat ChatManager { get; set; }

    internal static string AirDirectory { get; set; }
    internal static string GameDirectory { get; set; }

    internal static string LoginTheme { get; set; }

    internal static MainWindow MainWindow { get; set; }

    internal static IQueueManager QueueManager { get; set; }

    internal static Dictionary<int, GameQueueConfig> AvailableQueues { get; set; }

    internal static List<ChampionDTO> RiotChampions { get; set; }
    internal static List<MyChampDTO> AvailableChampions { get; set; }

    internal static SpellBookDTO Runes { get; private set; }
    internal static MasteryBookDTO Masteries { get; private set; }
    internal static SpellBookPageDTO SelectedRunePage { get; private set; }
    internal static MasteryBookPageDTO SelectedMasteryPage { get; private set; }

    internal static List<int> EnabledMaps { get; set; }

    internal static Settings Settings { get; set; }

    internal static Process GameProcess { get; set; }
    #endregion

    static Client() {
      if (!Directory.Exists(DataPath)) Directory.CreateDirectory(DataPath);
    }

    #region Initailization

    public static void PreInitialize(MainWindow window) {
      if (!Directory.Exists(DataPath))
        Directory.CreateDirectory(DataPath);
      Console.SetOut(TextWriter.Null);
      Console.SetError(TextWriter.Null);

      MainWindow = window;

      MFroehlich.League.RiotAPI.RiotAPI.UrlFormat
        = "https://na.api.pvp.net{0}&api_key=25434b55-24de-40eb-8632-f88cc02fea25";

      var parents = new[] { AirClientParentDir, GameClientParentDir };

      for (int i = 0; i < parents.Length; i++) {
        var versions = Directory.EnumerateDirectories(Path.Combine(parents[i], "releases"));
        Version newest = new Version(0, 0, 0, 0);
        foreach (var dir in versions) {
          Version parsed;
          if (Version.TryParse(Path.GetFileName(dir), out parsed) && parsed > newest) newest = parsed;
        }
        switch (i) {
          case 0: AirDirectory = Path.Combine(parents[i], "releases", newest.ToString(), "deploy"); break;
          case 1: GameDirectory = Path.Combine(parents[i], "releases", newest.ToString(), "deploy"); break;
        }
      }

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

      if (!File.Exists(FFMpegPath))
        using (var ffmpeg = new FileStream(FFMpegPath, FileMode.Create))
          ffmpeg.Write(LeagueClient.Properties.Resources.ffmpeg, 0, LeagueClient.Properties.Resources.ffmpeg.Length);
    }

    public static async Task<bool> Initialize(string user, string pass) {
      var context = RiotCalls.RegisterObjects();
      RtmpConn = new RtmpClient(new Uri("rtmps://" + Server + ":2099"), context, RtmpSharp.IO.ObjectEncoding.Amf3);
      RtmpConn.MessageReceived += RtmpConn_MessageReceived;
      await RtmpConn.ConnectAsync();

      var creds = new AuthenticationCredentials();
      creds.Username = user;
      creds.Password = pass;
      creds.ClientVersion = AirVersion;
      creds.Locale = Locale;
      creds.Domain = "lolclient.lol.riotgames.com";
      var queue = await RiotCalls.GetAuthKey(creds.Username, creds.Password, LoginQueue);
      creds.AuthToken = queue.Token;

      UserSession = await RiotCalls.LoginService.Login(creds);
      await RtmpConn.SubscribeAsync("my-rtmps", "messagingDestination",
        "bc", "bc-" + UserSession.AccountSummary.AccountId.ToString());
      await RtmpConn.SubscribeAsync("my-rtmps", "messagingDestination",
        "gn-" + UserSession.AccountSummary.AccountId.ToString(),
        "gn-" + UserSession.AccountSummary.AccountId.ToString());
      await RtmpConn.SubscribeAsync("my-rtmps", "messagingDestination",
        "cn-" + UserSession.AccountSummary.AccountId.ToString(),
        "cn-" + UserSession.AccountSummary.AccountId.ToString());

      bool authed = await RtmpConn.LoginAsync(creds.Username.ToLower(), UserSession.Token);
      LoginPacket = await RiotCalls.ClientFacadeService.GetLoginDataPacketForUser();
      string state = await RiotCalls.AccountService.GetAccountState();
      Connected = true;

      new System.Threading.Thread(() => {
        RiotCalls.MatchmakerService.GetAvailableQueues()
          .ContinueWith(GotQueues);
        RiotCalls.InventoryService.GetAvailableChampions()
          .ContinueWith(GotChampions);
        Runes = LoginPacket.AllSummonerData.SpellBook;
        Masteries = LoginPacket.AllSummonerData.MasteryBook;
        SelectedRunePage = Runes.BookPages.FirstOrDefault(p => p.Current);
        SelectedMasteryPage = Masteries.BookPages.FirstOrDefault(p => p.Current);
      }).Start();

      EnabledMaps = new List<int>();
      foreach (var item in LoginPacket.ClientSystemStates.gameMapEnabledDTOList)
        EnabledMaps.Add((int) item["gameMapId"]);

      if (!state.Equals("ENABLED")) TryBreak("state is not ENABLED");

      Settings.ProfileIcon = LoginPacket.AllSummonerData.Summoner.ProfileIconId;
      Settings.SummonerName = LoginPacket.AllSummonerData.Summoner.Name;

      return state.Equals("ENABLED");
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

    private static void GotQueues(Task<GameQueueConfig[]> Task) {
      AvailableQueues = new Dictionary<int, GameQueueConfig>();
      foreach (var item in Task.Result) AvailableQueues.Add((int) item.Id, item);
      //new Thread(PlaySelectPage.Setup).Start();
    }

    #endregion

    #region Riot Client Methods
    /// <summary>
    /// Launches the league of legends client and joins an active game
    /// </summary>
    /// <param name="creds">The credentials for joining the game</param>
    public static void JoinGame(PlayerCredentialsDto creds) {
      //"8394" "LoLLauncher.exe" "" "ip port key id"
      var info = new ProcessStartInfo(Path.Combine(GameDirectory, "League of Legends.exe"));
      var str = $"{creds.ServerIp} {creds.ServerPort} {creds.EncryptionKey} {creds.SummonerId}";
      info.Arguments = string.Format("\"{0}\" \"{1}\" \"{2}\" \"{3}\"", "8394", "LoLLauncher.exe", "", str);
      info.WorkingDirectory = GameDirectory;
      GameProcess = Process.Start(info);

      App.Current.Dispatcher.Invoke(MainWindow.ShowInGamePage);
      ChatManager.UpdateStatus(ChatStatus.inGame);
    }

    /// <summary>
    /// Selects a mastery page as the default selected page for your account and
    /// updates the contents of the local and remote mastery books
    /// </summary>
    /// <param name="page">The page to select</param>
    public static void SelectMasteryPage(MasteryBookPageDTO page) {
      if (page == SelectedMasteryPage) return;
      RiotCalls.MasteryBookService.SelectDefaultMasteryBookPage(page);
      foreach (var item in Masteries.BookPages) item.Current = false;
      page.Current = true;
      SelectedMasteryPage = page;
    }

    /// <summary>
    /// Selects a rune page as the default selected page for your account and
    /// updates the contents of the local and remote spell books
    /// </summary>
    /// <param name="page"></param>
    public static void SelectRunePage(SpellBookPageDTO page) {
      if (page == SelectedRunePage) return;
      RiotCalls.SpellBookService.SelectDefaultSpellBookPage(page);
      foreach (var item in Runes.BookPages) item.Current = false;
      page.Current = true;
      SelectedRunePage = page;
    }

    private static Dictionary<string, Alert> invites = new Dictionary<string, Alert>();
    public static void ShowInvite(InvitationRequest invite) {
      if (invite.InvitationState.Equals("ACTIVE")) {
        var payload = JSON.ParseObject(invite.GameMetaData);
        string type = payload["gameType"];
        Alert alert;
        if(payload["gameTypeConfigId"] == 12) {
          alert = AlertFactory.TeambuilderInvite(invite.Inviter.summonerName);
          alert.Handled += (src, e2) => {
            if (e2.Data as bool? ?? false) {
              var lobby = new CapLobbyPage(false);
              RiotCalls.GameInvitationService.Accept(invite.InvitationId).ContinueWith(t => lobby.GotLobbyStatus(t.Result));
              RiotCalls.CapService.JoinGroupAsInvitee((string) payload["groupFinderId"]);
              QueueManager.ShowPage(lobby);
            } else RiotCalls.GameInvitationService.Decline(invite.InvitationId);
          };
        } else {
          switch (type) {
            case "PRACTICE_GAME":
              alert = AlertFactory.CustomInvite(invite.Inviter.summonerName);
              alert.Handled += (src, e2) => {
                if (e2.Data as bool? ?? false) {
                  var lobby = new CustomLobbyPage();
                  RiotCalls.GameInvitationService.Accept(invite.InvitationId);
                  QueueManager.ShowPage(lobby);
                } else RiotCalls.GameInvitationService.Decline(invite.InvitationId);
              };
              break;
            case "NORMAL_GAME":
              alert = AlertFactory.NormalInvite(invite.Inviter.summonerName, GameMode.Values[payload["gameMode"]]);
              alert.Handled += (src, e2) => {
                if (e2.Data as bool? ?? false) {
                  var lobby = new DefaultLobbyPage(new MatchMakerParams { QueueIds = new int[] { payload["queueId"] } });
                  RiotCalls.GameInvitationService.Accept(invite.InvitationId).ContinueWith(t => lobby.GotLobbyStatus(t.Result));
                  QueueManager.ShowPage(lobby);
                } else RiotCalls.GameInvitationService.Decline(invite.InvitationId);
              };
              break;
            default: alert = null; break;
          }
        }
        invites[invite.InvitationId] = alert;
        QueueManager.ShowNotification(alert);
      }
    }

    public static void Logout() {
      if (Connected) {
        Client.SaveSettings(Client.Settings.Username, JSONObject.From(Client.Settings));
        RiotCalls.GameService.QuitGame();
        RiotCalls.LoginService.Logout().ContinueWith(t => RtmpConn.LogoutAsync().ContinueWith(t2 => RtmpConn.Close()));
        Connected = false;
      }
      ChatManager?.Logout();
      MainWindow.PatchComplete();
    }
    #endregion

    #region My Client Methods

    public static JSONObject LoadSettings(string name) {
      name = name.RemoveAllWhitespace();
      var file = Path.Combine(DataPath, name + ".settings");
      if (!File.Exists(file))
        return new JSONObject();
      var json = JSON.ParseObject(File.ReadAllBytes(file));
      return json;
    }

    public static void SaveSettings(string name, JSONObject json) {
      name = name.RemoveAllWhitespace();
      var file = Path.Combine(DataPath, name + ".settings");
      File.WriteAllText(file, JSON.Stringify(json, 2, 0));
    }

    private static TextWriter LogDebug = Console.Out;
    public static void Log(object msg) {
      lock (LogDebug) {
        using (var log = new StreamWriter(File.Open(LogFilePath, FileMode.Append))) {
          LogDebug.WriteLine(msg);
          log.WriteLine(msg);
        }
      }
    }

    public static void Log(string msg, params object[] args) {
      if (args.Length == 0) Log((object) msg);
      else Log((object) string.Format(msg, args));
    }

    public static void TryBreak(string reason) {
      Log("Attempt Break: " + reason);
      if (Debugger.IsAttached) Debugger.Break();
    }

    public static long GetMilliseconds() => (long) DateTime.UtcNow.Subtract(Epoch).TotalMilliseconds;

    #endregion

    public static void RtmpConn_MessageReceived(object sender, MessageReceivedEventArgs e) {
      try {
        if (MainWindow.HandleMessage(e)) return;
      } catch (Exception x) {
        Log("Exception while dispatching message: " + x.Message);
        TryBreak(x.Message);
      }

      ClientDynamicConfigurationNotification config;
      LcdsServiceProxyResponse response;
      InvitationRequest invite;

      try {
        if ((response = e.Body as LcdsServiceProxyResponse) != null) {
          if (response.status.Equals("ACK"))
            Log("Acknowledged call of method {0} [{1}]", response.methodName, response.messageId);
          else if (response.messageId != null && RiotCalls.Delegates.ContainsKey(response.messageId)) {
            RiotCalls.Delegates[response.messageId](response);
            RiotCalls.Delegates.Remove(response.messageId);
          } else {
            Log("Unhandled LCDS response of method {0} [{1}], {2}", response.methodName, response.messageId, response.payload);
          }
        } else if ((config = e.Body as ClientDynamicConfigurationNotification) != null) {
          Log("Received Configuration Notification");
        } else if ((invite = e.Body as InvitationRequest) != null) {
          ShowInvite(invite);
        } else if (response == null) {
          Log("Receive [{1}, {2}]: '{0}'", e.Body, e.Subtopic, e.ClientId);
        }
      } catch (Exception x) {
        Log("Exception while handling message: " + x.Message);
        TryBreak(x.Message);
      }
    }
  }
}
