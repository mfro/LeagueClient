using MFroehlich.League;
using MFroehlich.League.Assets;
using MFroehlich.League.RiotAPI;
using RiotClient.Chat;
using RiotClient.com.riotgames.other;
using RiotClient.Lobbies;
using RiotClient.Riot.Platform;
using RiotClient.Riot.Team;
using RiotClient.Settings;
using RtmpSharp.Messaging;
using RtmpSharp.Net;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Authentication;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
using MyChampDTO = MFroehlich.League.DataDragon.ChampionDto;

namespace RiotClient {
  public class Session {
    public static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public static readonly Region Region = Region.NA;
    public static WebClient WebClient = new WebClient();

    public static readonly string
      RiotGamesDir = @"C:\Riot Games\" + (Region == Region.PBE ? "PBE" : "League of Legends"),
      Locale = "en_US",
      DataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\MFro\LeagueClient\",
      SettingsFile = Path.Combine(DataPath, "settings.xml"),
      FFMpegPath = Path.Combine(DataPath, "ffmpeg.exe"),
      LoginVideoPath = Path.Combine(DataPath, "login.mp4"),
      LoginStaticPath = Path.Combine(DataPath, "back.png"),
      LogFilePath = Path.Combine(DataPath, "log.txt");

    public static RiotVersion Latest { get; set; }
    public static RiotVersion Installed { get; set; }

    public static string LoginTheme { get; set; }
    public static Session Current { get; private set; }

    public event EventHandler<InvitationRequest> Invited;

    public Queue CurrentQueue { get; internal set; }
    public Lobby CurrentLobby { get; internal set; }
    public RiotChat ChatManager { get; internal set; }

    public dynamic Credentials { get; set; }
    public Account Account { get; private set; }
    public LoginQueueDto LoginQueue { get; private set; }

    public List<ChampionDTO> RiotChampions { get; private set; }
    public List<MyChampDTO> AvailableChampions { get; private set; }

    public PlayerDTO RankedTeamInfo { get; private set; }

    public bool Connected { get; private set; }

    public UserSettings Settings { get; set; }
    public SummonerCache SummonerCache { get; private set; }

    public Dictionary<int, GameQueueConfig> AvailableQueues { get; private set; }

    internal RtmpClient RtmpConn { get; set; }

    internal SummonerLeaguesDTO Leagues { get; set; }

    internal string ReconnectToken { get; set; }

    internal List<int> EnabledMaps { get; set; }


    internal AsyncProperty<RiotAPI.CurrentGameAPI.CurrentGameInfo> CurrentGameInfo { get; set; }


    private const string SettingsKey = "GlobalSettings";
    private static GlobalSettings settings = LoadSettings<GlobalSettings>(SettingsKey);

    private Session() { }

    #region Initialization

    public static async void Initialize() {
      Log("Initialize");
      if (!Directory.Exists(DataPath))
        Directory.CreateDirectory(DataPath);

      DataDragon.Locale = Locale;
      if (!DataDragon.IsCurrent) DataDragon.Update();

      RiotAPI.UrlFormat = "https://na.api.pvp.net{0}&api_key=25434b55-24de-40eb-8632-f88cc02fea25";

      Installed = await RiotVersion.GetInstalledVersion(Region, RiotGamesDir);
      Latest = await RiotVersion.GetLatestVersion(Region);

      var theme = Latest.GetFiles("/files/theme.properties").Single();
      var content = await WebClient.DownloadStringTaskAsync(theme.Url);
      LoginTheme = content.Substring("themeConfig=", ",");

      if (!File.Exists(FFMpegPath))
        using (var ffmpeg = new FileStream(FFMpegPath, FileMode.Create))
          ffmpeg.Write(Properties.Resources.ffmpeg, 0, Properties.Resources.ffmpeg.Length);

      Log(DataDragon.CurrentVersion);
      Log($"Air: {Installed.AirVersion} / {Latest.AirVersion}");
      Log($"Game: {Installed.GameVersion} / {Latest.GameVersion}");
      Log($"Solution: {Installed.SolutionVersion} / {Latest.SolutionVersion}");

      new Thread(CreateLoginTheme).Start();
    }

    public static async Task<Session> Login(string user, string pass) {
      var client = new Session();
      client.LoginQueue = await RiotServices.GetAuthKey(Region, user, pass);
      if (client.LoginQueue.Token == null) throw new AuthenticationException();

      var context = RiotServices.RegisterObjects();
      client.RtmpConn = RiotServices.Client = new RtmpClient(new Uri("rtmps://" + Region.MainServer + ":2099"), context, RtmpSharp.IO.ObjectEncoding.Amf3);
      client.RtmpConn.MessageReceived += client.RtmpConn_MessageReceived;
      client.RtmpConn.Disconnected += client.RtmpConn_Disconnected;
      await client.RtmpConn.ConnectAsync();

      var creds = new AuthenticationCredentials();
      creds.Username = user;
      creds.Password = pass;
      creds.ClientVersion = DataDragon.CurrentVersion;
      creds.Locale = Locale;
      creds.Domain = "lolclient.lol.riotgames.com";
      creds.AuthToken = client.LoginQueue.Token;

      var loginSession = await RiotServices.LoginService.Login(creds);

      var bc = $"bc-{loginSession.AccountSummary.AccountId}";
      var gn = $"gn-{loginSession.AccountSummary.AccountId}";
      var cn = $"cn-{loginSession.AccountSummary.AccountId}";
      var tasks = new[] {
        client.RtmpConn.SubscribeAsync("my-rtmps", "messagingDestination", "bc", bc),
        client.RtmpConn.SubscribeAsync("my-rtmps", "messagingDestination", gn, gn),
        client.RtmpConn.SubscribeAsync("my-rtmps", "messagingDestination", cn, cn),
      };
      await Task.WhenAll(tasks);

      bool authed = await client.RtmpConn.LoginAsync(creds.Username.ToLower(), loginSession.Token);
      string state = await RiotServices.AccountService.GetAccountState();
      client.Account = new Account(loginSession, await RiotServices.ClientFacadeService.GetLoginDataPacketForUser());

      client.Leagues = await RiotServices.LeaguesService.GetAllLeaguesForPlayer(client.Account.SummonerID);
      client.Connected = true;
      client.ReconnectToken = Convert.ToBase64String(Encoding.UTF8.GetBytes(client.Account.Name + ":" + client.LoginQueue.Token));

      client.StartHeartbeat();
      new Thread(() => {
        RiotServices.MatchmakerService.GetAvailableQueues().ContinueWith(client.GotQueues);
        RiotServices.InventoryService.GetAvailableChampions().ContinueWith(client.GotChampions);
        RiotServices.SummonerTeamService.CreatePlayer().ContinueWith(client.GotRankedTeamInfo);

        RiotServices.GameInvitationService.GetPendingInvitations().ContinueWith(t => {
          foreach (var invite in t.Result) {
            if (invite is InvitationRequest)
              client.OnInvited((InvitationRequest) invite);
          }
        });
      }).Start();

      client.EnabledMaps = new List<int>();
      foreach (var item in client.Account.LoginPacket.ClientSystemStates.gameMapEnabledDTOList)
        client.EnabledMaps.Add((int) item["gameMapId"]);

      if (state?.Equals("ENABLED") != true) {
        Console.WriteLine(state);
        RiotServices.Client = null;
        throw new AuthenticationException(state);
      }

      client.Settings = LoadSettings<UserSettings>(user);
      client.Settings.ProfileIcon = client.Account.ProfileIconID;
      client.Settings.SummonerName = client.Account.Name;
      client.SummonerCache = new SummonerCache();

      Current = client;

      client.ChatManager = new RiotChat();

      return client;
    }

    private static void CreateLoginTheme() {
      if (!LoginTheme.Equals(settings.Theme) || !File.Exists(LoginVideoPath) || !File.Exists(LoginStaticPath)) {
        var png = Latest.AirFiles.FirstOrDefault(f => f.Url.AbsolutePath.EndsWith($"/files/mod/lgn/themes/{LoginTheme}/cs_bg_champions.png"));
        using (var web = new WebClient())
          web.DownloadFile(png.Url, LoginStaticPath);

        var file = Path.GetTempFileName();
        using (var web = new WebClient()) {
          var flv = Latest.AirFiles.FirstOrDefault(f => f.Url.AbsolutePath.EndsWith($"/files/mod/lgn/themes/{LoginTheme}/flv/login-loop.flv"));
          web.DownloadFile(flv.Url, file);
        }
        var info = new ProcessStartInfo {
          FileName = FFMpegPath,
          Arguments = $"-i \"{file}\" \"{LoginVideoPath}\"",
          UseShellExecute = false,
          CreateNoWindow = true,
          RedirectStandardError = true,
          RedirectStandardOutput = true,
        };
        File.Delete(LoginVideoPath);
        Process.Start(info);
        settings.Theme = LoginTheme;
        SaveSettings(SettingsKey, settings);
      }
    }

    private static System.Timers.Timer HeartbeatTimer;
    private static int Heartbeats;

    private void StartHeartbeat() {
      HeartbeatTimer = new System.Timers.Timer();
      HeartbeatTimer.Elapsed += HeartbeatTimer_Elapsed;
      HeartbeatTimer.Interval = 120000;
      HeartbeatTimer.Start();
      HeartbeatTimer_Elapsed(HeartbeatTimer, null);
    }

    private async void HeartbeatTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e) {
      if (Connected) {
        var result = await RiotServices.LoginService.PerformLCDSHeartBeat(
          (int) Account.AccountID,
          Account.LoginSession.Token,
          Heartbeats,
          DateTime.Now.ToString("ddd MMM d yyyy HH:mm:ss 'GMT-0700'"));
        if (!result.Equals("5")) {
          Console.WriteLine("Heartbeat unexpected");
        }
        Heartbeats++;
      }
    }

    private void GotChampions(Task<ChampionDTO[]> Champs) {
      if (Champs.IsFaulted) {
        if (Debugger.IsAttached) Debugger.Break();
        return;
      }
      RiotChampions = new List<ChampionDTO>(Champs.Result);
      AvailableChampions = new List<MyChampDTO>();
      foreach (var item in Champs.Result)
        AvailableChampions.Add(DataDragon.GetChampData(item.ChampionId));
    }

    private void GotQueues(Task<GameQueueConfig[]> Task) {
      AvailableQueues = new Dictionary<int, GameQueueConfig>();
      foreach (var item in Task.Result)
        if (Account.Level >= item.MinLevel && Account.Level <= item.MaxLevel)
          AvailableQueues.Add(item.Id, item);
    }

    private void GotRankedTeamInfo(Task<PlayerDTO> obj) {
      RankedTeamInfo = obj.Result;
    }

    #endregion

    public void JoinGame() {
      //"8394" "LoLPatcher.exe" "" "ip port key id"
      if (Process.GetProcessesByName("League of Legends").Length > 0) {
        ChatManager.Status = ChatStatus.inGame;
        new Thread(GetCurrentGame).Start();
        return;
      }

      var game = Path.Combine(RiotGamesDir, RiotVersion.SolutionPath, Latest.SolutionVersion.ToString(), "deploy");
      var lolclient = Path.Combine(RiotGamesDir, RiotVersion.AirPath, Latest.AirVersion.ToString(), "deploy", "LolClient.exe");

      var info = new ProcessStartInfo(Path.Combine(game, "League of Legends.exe"));
      var str = $"{Credentials.ServerIp} {Credentials.ServerPort} {Credentials.EncryptionKey} {Credentials.SummonerId}";
      info.Arguments = string.Format("\"{0}\" \"{1}\" \"{2}\" \"{3}\"", "8394", "LoLPatcher.exe", lolclient, str);
      info.WorkingDirectory = game;
      Process.Start(info);

      ChatManager.Status = ChatStatus.inGame;
      new Thread(GetCurrentGame).Start();
    }

    public void ShowInvite(InvitationRequest invite) {
      if (invite.InvitationState.Equals("ACTIVE")) {
        var user = RiotChat.GetUser(invite.Inviter.summonerId);
        if (ChatManager.Friends.ContainsKey(user)) {
          ChatManager.Friends[user].Invite = new Invitation(invite);
        } else {
          OnInvited(invite);
        }
      }
    }

    public void SendInvite(long summoner) {
      if (CurrentLobby is QueueLobby) {
        (CurrentLobby as QueueLobby).Invite(summoner);
      } else if (CurrentLobby is CustomLobby) {
        (CurrentLobby as CustomLobby).Invite(summoner);
      }
    }

    public void Logout() {
      if (Connected) {
        try {
          SaveSettings(Settings.Username, Settings);
          RtmpConn.MessageReceived -= RtmpConn_MessageReceived;
          HeartbeatTimer.Dispose();
          new Thread(async () => {
            Connected = false;
            CurrentQueue?.Cancel();
            CurrentLobby?.Dispose();
            await RiotServices.LoginService.Logout();
            await RtmpConn.LogoutAsync();
            RtmpConn.Close();
          }).Start();
        } catch { }
      }
      ChatManager?.Dispose();
    }

    #region Util methods

    private void GetCurrentGame() {
      Thread.Sleep(20000);
      CurrentGameInfo = new Task<RiotAPI.CurrentGameAPI.CurrentGameInfo>(() => {
        try {
          return RiotAPI.CurrentGameAPI.BySummoner("NA1", Account.SummonerID);
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

    public static void ThrowException(Exception x, string details = null) {
      if (details != null) Log(details);
      Log(x);
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

    public void RtmpConn_MessageReceived(object sender, MessageReceivedEventArgs e) {
      try {
        if (CurrentQueue != null && CurrentQueue.HandleMessage(e))
          return;

        if (CurrentLobby != null && CurrentLobby.HandleMessage(e))
          return;
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
          //TODO End of game
        } else {
          Log($"Receive [{e.Subtopic}, {e.ClientId}]: '{e.Body}'");
        }
      } catch (Exception x) {
        ThrowException(x, "Exception while handling message");
      }
    }

    private async void RtmpConn_Disconnected(object sender, EventArgs e) {
      Connected = false;
      await RtmpConn.RecreateConnection(ReconnectToken);

      var bc = $"bc-{Account.AccountID}";
      var gn = $"gn-{Account.AccountID}";
      var cn = $"cn-{Account.AccountID}";
      var tasks = new[] {
        RtmpConn.SubscribeAsync("my-rtmps", "messagingDestination", "bc", bc),
        RtmpConn.SubscribeAsync("my-rtmps", "messagingDestination", gn, gn),
        RtmpConn.SubscribeAsync("my-rtmps", "messagingDestination", cn, cn),
      };
      await Task.WhenAll(tasks);
      Connected = true;
    }

    private void OnInvited(InvitationRequest invite) {
      Invited?.Invoke(this, invite);
    }
  }
}
