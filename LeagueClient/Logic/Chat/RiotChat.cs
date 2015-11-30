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

    private void ResetList() {
      var list = Friends.Values.Where(u => !u.IsOffline).OrderBy(u => u.GetValue()).ToList();
      for (int i = 0; i < Math.Max(FriendList.Count, list.Count); i++) {
        if (i >= list.Count) {
          FriendList.RemoveAt(i);
          i--;
        } else if (i == FriendList.Count) {
          FriendList.Add(list[i]);
        } else if (FriendList[i] != list[i]) {
          FriendList[i] = list[i];
        }
      }
    }

    private void UpdateProc(object src, ElapsedEventArgs args) {
      try { App.Current.Dispatcher.Invoke(ResetList); } catch { timer.Dispose(); }
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
      App.Current.Dispatcher.Invoke(ResetList);
    }

    private void OnChatOpen(object src, EventArgs args) {
      var user = ((ChatConversation) src).User;
      foreach (var chat in OpenChats)
        if (!chat.User.Equals(user)) chat.Open = false;
        else chat.ChatSendBox.MyFocus();
    }

    private void OnChatClose(object src, EventArgs args) {
      var user = ((ChatConversation) src).User;
      Friends[user].Conversation.Open = false;
      OpenChats.Remove(Friends[user].Conversation);
    }

    private void OnReceiveMessage(object sender, Message msg) {
      if (!Friends.ContainsKey(msg.From.User)) return;
      string user = msg.From.User;
      if (msg.From.User.Equals(msg.To.User)) {
        return;
      }
      App.Current.Dispatcher.Invoke(() => {
        var convo = Friends[user].Conversation;
        if (!OpenChats.Contains(convo)) {
          AddChat(Friends[user]);
          convo.Open = false;
        }
        if (!convo.Open) {
          convo.Unread = true;
        }
        convo.History += $"[{Friends[user].User.Nickname}]: {msg.Body}\n";
      });
    }

    private void OnSendMessage(object sender, MessageSentEventArgs msg) {
      conn.Message(msg.User + "@pvp.net", msg.Message);
      Friends[msg.User].Conversation.History += $"[{Client.LoginPacket.AllSummonerData.Summoner.Name}]: {msg.Message}\n";
    }

    private void OnRosterItem(object sender, Item item) {
      if (!Friends.ContainsKey(item.JID.User)) {
        Application.Current.Dispatcher.MyInvoke(CreateChatFriend, item);
      }
      if (!fullyAuthed) OnPrimarySessionChange(sender, item.JID);
      //if (!Users.ContainsKey(item.JID.User)) {
      //  Users.Add(item.JID.User, item);

      //  App.Current.Dispatcher.Invoke(() => {
      //    var convo = new ChatConversation(item.Nickname, item.JID.User);
      //    convo.MessageSent += OnSendMessage;
      //    convo.ChatOpened += OnChatOpen;
      //    convo.ChatClosed += OnChatClose;
      //    var friend = new Friend(item, convo);
      //    friend.MouseUp += (src, e) => OnChatAdd(friend);
      //    Friends.Add(item.JID.User, friend);
      //  });
      //}
      //if (!fullyAuthed)
      //  OnPrimarySessionChange(sender, item.JID);
    }
    #endregion

    private void CreateChatFriend(Item item) {
      var chatFriend = new ChatFriend(item);
      chatFriend.Conversation.MessageSent += OnSendMessage;
      chatFriend.Conversation.ChatOpened += OnChatOpen;
      chatFriend.Conversation.ChatClosed += OnChatClose;
      Friends.Add(item.JID.User, chatFriend);
    }

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

    /// <summary>
    /// Minimizes any open chat conversations
    /// </summary>
    public void CloseAll() {
      foreach (var item in OpenChats) item.Open = false;
    }

    public void AddChat(ChatFriend friend) {
      if (!OpenChats.Contains(friend.Conversation))
        OpenChats.Add(friend.Conversation);
      friend.Conversation.Open = !friend.Conversation.Open;
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