using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Xml.Linq;
using jabber;
using jabber.client;
using jabber.connection;
using jabber.protocol.client;
using jabber.protocol.iq;
using LeagueClient.ClientUI.Controls;

namespace LeagueClient.Logic.Chat {
  public sealed class RiotChat : IDisposable {
    public event EventHandler Tick;
    private static readonly List<ChatStatus> DndStatuses = new List<ChatStatus> {
      ChatStatus.championSelect, ChatStatus.tutorial, ChatStatus.inGame, ChatStatus.inQueue, ChatStatus.spectating
    };

    public event EventHandler<StatusUpdatedEventArgs> StatusUpdated;
    public event EventHandler<Message> MessageReceived;

    public State ChatState { get; private set; }
    public Dictionary<string, ChatFriend> Friends { get; } = new Dictionary<string, ChatFriend>();
    public BindingList<ChatConversation> OpenChats { get; } = new BindingList<ChatConversation>();
    public BindingList<ChatFriend> FriendList { get; } = new BindingList<ChatFriend>();

    private JabberClient conn;
    private RosterManager Roster = new RosterManager();
    private PresenceManager Presence = new PresenceManager();
    private ConferenceManager Conference = new ConferenceManager();
    private bool fullyAuthed;
    private Timer timer;

    public ChatStatus Status { get; private set; }
    public StatusShow Show { get; private set; }

    public RiotChat(string user, string pass) {
      Status = ChatStatus.outOfGame;
      Show = StatusShow.Chat;
      conn = new JabberClient {
        Server = "pvp.net",
        Port = 5223,
        SSL = true,
        AutoReconnect = 30,
        KeepAlive = 10,
        NetworkHost = Client.Region.ChatServer,
        User = user,
        Password = "AIR_" + pass,
        Resource = "xiff",
        AutoPresence = false
      };
      conn.OnInvalidCertificate += (src, cert, poop, poo2) => true;
      conn.OnMessage += OnReceiveMessage;
      conn.OnAuthenticate += s => UpdateStatus(Client.Settings.ChatStatus);
      ChatState = State.Connecting;
      conn.Connect();
      Roster.Stream = conn;
      Roster.AutoSubscribe = true;
      Roster.AutoAllow = AutoSubscriptionHanding.AllowAll;
      Roster.OnRosterItem += OnRosterItem;
      Roster.OnRosterEnd += src => {
        fullyAuthed = true;
        Presence.OnPrimarySessionChange += OnPrimarySessionChange;
        if (conn.IsAuthenticated)
          SendPresence();
      };
      Presence.Stream = conn;
      Conference.Stream = conn;

      timer = new Timer(1000) { AutoReset = true };
      timer.Elapsed += UpdateProc;
      timer.Start();
    }

    private void ResetList(bool force) {
      var list = Friends.Values.Where(u => !u.IsOffline && u.Status != null).OrderBy(u => u.GetValue()).ToList();
      for (int i = 0; i < Math.Max(FriendList.Count, list.Count); i++) {
        if (i >= list.Count) {
          FriendList.RemoveAt(i);
          i--;
        } else if (i == FriendList.Count) {
          FriendList.Add(list[i]);
        } else if (force || FriendList[i] != list[i]) {
          FriendList[i] = list[i];
        }
      }
    }

    private void UpdateProc(object src, ElapsedEventArgs args) {
      try { Application.Current.Dispatcher.MyInvoke(ResetList, false); } catch { timer.Dispose(); }
      Tick?.Invoke(this, new EventArgs());
    }

    #region Event Handlers
    private void OnPrimarySessionChange(object sender, jabber.JID bare) {
      if (!Friends.ContainsKey(bare.User)) {
        if (!bare.User.Equals(conn.JID.User)) {
          var p = Presence.GetAll(bare);
          if (p.Any(s2 => Client.LoginPacket.AllSummonerData.Summoner.Name.Equals(s2.From.Resource))) return;
          SendPresence();
        }
        return;
      }

      var s = Presence.GetAll(bare);
      if (s.Length == 0) Friends[bare.User].UpdatePresence(null);
      else Friends[bare.User].UpdatePresence(s[0]);
      Application.Current.Dispatcher.MyInvoke(ResetList, false);
    }

    private void OnReceiveMessage(object sender, Message msg) {
      if (!Friends.ContainsKey(msg.From.User)) return;
      if (msg.From.User.Equals(msg.To.User)) return;
      Friends[msg.From.User].ReceiveMessage(msg.Body);
      MessageReceived?.Invoke(this, msg);
    }

    private void OnRosterItem(object sender, Item item) {
      if (!Friends.ContainsKey(item.JID.User)) {
        Application.Current.Dispatcher.Invoke(() => Friends.Add(item.JID.User, new ChatFriend(item)));
      }
      if (!fullyAuthed) OnPrimarySessionChange(sender, item.JID);
    }
    #endregion

    #region Status
    /// <summary>
    /// Updates the current status string
    /// </summary>
    /// <param name="message">The status message to display</param>
    public void UpdateStatus(string message) {
      Client.Settings.ChatStatus = message;
      SendPresence();
    }

    public void UpdateStatus(ChatStatus status) {
      this.Status = status;
      SendPresence();
    }

    public void UpdateStatus(StatusShow show) {
      this.Show = show;
      SendPresence();
    }

    public void SendPresence() {
      var status = new LeagueStatus(Client.Settings.ChatStatus, Status);
      var computed = DndStatuses.Contains(Status) ? StatusShow.Dnd : Show;

      var args = new StatusUpdatedEventArgs(status, PresenceType.available, computed);
      conn.Presence(args.PresenceType, args.Status.ToXML(), args.Show.ToString().ToLower(), 0);
      StatusUpdated?.Invoke(this, args);
    }
    #endregion

    #region Chat Rooms
    private static string GetObfuscatedChatroomName(string Subject, string Type) {
      int bitHack = 0;
      byte[] data = System.Text.Encoding.UTF8.GetBytes(Subject);
      byte[] result;
      var sha = new SHA1CryptoServiceProvider();
      result = sha.ComputeHash(data);
      string obfuscatedName = "";
      int incrementValue = 0;
      while (incrementValue < result.Length) {
        bitHack = result[incrementValue];
        obfuscatedName = obfuscatedName + Convert.ToString(((uint) (bitHack & 240) >> 4), 16);
        obfuscatedName = obfuscatedName + Convert.ToString(bitHack & 15, 16);
        incrementValue = incrementValue + 1;
      }
      obfuscatedName = Regex.Replace(obfuscatedName, @"/\s+/gx", "");
      obfuscatedName = Regex.Replace(obfuscatedName, @"/[^a-zA-Z0-9_~]/gx", "");
      return Type + "~" + obfuscatedName;
    }

    private static string GetChatroomJID(string obfuscatedName, bool isPublic, string pass = null) {
      if (!isPublic) return obfuscatedName + "@sec.pvp.net";
      else if (pass == null) return obfuscatedName + "@lvl.pvp.net";
      else return obfuscatedName + "@conference.pvp.net";
    }

    public static JID GetTeambuilderRoom(string groupId, string pass) {
      return new JID(GetChatroomJID(GetObfuscatedChatroomName(groupId, "cp"), true, pass));
    }

    public static JID GetCustomRoom(string roomname, double roomId, string pass) {
      return new JID(GetChatroomJID(GetObfuscatedChatroomName(roomname.ToLower() + (int) roomId, "ap"), false, pass));
    }

    public static JID GetLobbyRoom(string inviteId, string pass) {
      return new JID(GetChatroomJID(GetObfuscatedChatroomName(inviteId.ToLower(), "ag"), false, pass));
    }

    public Room JoinRoom(JID jid) {
      var room = Conference.GetRoom(jid);
      room.Nickname = Client.LoginPacket.AllSummonerData.Summoner.Name;
      return room;
    }
    #endregion

    /// <summary>
    /// Parses the internal summoner ID of a friend from the JID used in the XMPP chat service
    /// </summary>
    /// <param name="user">The summoner's JID</param>
    /// <returns>The summoner ID</returns>
    public static double GetSummonerId(JID user) {
      double d;
      if (double.TryParse(user.User.Substring(3), out d)) return d;
      else throw new FormatException(user.User + " is not correctly formatted");
    }

    public ChatFriend GetUser(long summonerId) {
      var user = "sum" + summonerId;
      if (Friends.ContainsKey(user)) return Friends[user];
      else return null;
    }

    /// <summary>
    /// Minimizes any open chat conversations
    /// </summary>
    public void CloseAll() {
      foreach (var item in OpenChats) item.Open = false;
    }

    /// <summary>
    /// Sends a message
    /// </summary>
    /// <param name="user">The user to send the message to</param>
    /// <param name="message">The message to send</param>
    public void SendMessage(JID user, string message) {
      conn.Message(user.User + "@pvp.net", message);
    }

    public void ForceUpdate() {
      Application.Current.Dispatcher.MyInvoke(ResetList, true);
    }

    public void Logout() {
      conn.Close();
    }

    public void Dispose() {
      timer.Dispose();
      Conference.Dispose();
      Presence.Dispose();
      Roster.Dispose();
    }

    public enum State {
      Disconnected, Connecting, Connected
    }
  }

  public class StatusUpdatedEventArgs : EventArgs {
    public LeagueStatus Status { get; set; }
    public PresenceType PresenceType { get; set; }
    public StatusShow Show { get; set; }

    public StatusUpdatedEventArgs(LeagueStatus status, PresenceType presenceType, StatusShow show) {
      Status = status;
      PresenceType = presenceType;
      Show = show;
    }
  }
}