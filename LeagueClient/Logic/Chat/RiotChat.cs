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
using System.Windows.Input;
using System.Xml.Linq;
using jabber;
using jabber.client;
using jabber.connection;
using jabber.protocol.client;
using jabber.protocol.iq;
using LeagueClient.ClientUI.Controls;
using LeagueClient.Logic.Riot;

namespace LeagueClient.Logic.Chat {
  public class RiotChat {
    public event EventHandler<List<Friend>> ChatListUpdated;

    public State ChatState { get; private set; }
    public BindingList<ChatConversation> OpenChats { get; private set; }
    public Dictionary<string, Item> Users { get; } = new Dictionary<string, Item>();

    private Dictionary<string, Friend> Friends = new Dictionary<string, Friend>();

    private JabberClient conn;
    private RosterManager Roster = new RosterManager();
    private PresenceManager Presence = new PresenceManager();
    private ConferenceManager Conference = new ConferenceManager();
    private bool fullyAuthed;
    private Timer timer;

    public GameStatus Status { get; private set; }
    public StatusShow Show { get; private set; }
    public string Message { get; private set; }

    public RiotChat(string user, string pass) {
      App.Current.Dispatcher.Invoke(() => {
        OpenChats = new BindingList<ChatConversation>();
      });

      Status = LeagueStatus.Idle;
      Show = StatusShow.Chat;
      Message = "";
      conn = new JabberClient {
        Server = "pvp.net",
        Port = 5223,
        SSL = true,
        AutoReconnect = 30,
        KeepAlive = 10,
        NetworkHost = Client.ChatServer,
        User = user,
        Password = "AIR_"+pass,
        Resource = "xiff",
      };
      conn.OnInvalidCertificate += (src, cert, poop, poo2) => true;
      conn.OnMessage += OnReceiveMessage;
      conn.OnAuthenticate += s => UpdateStatus(Message);
      ChatState = State.Connecting;
      conn.Connect();
      Roster.Stream = conn;
      Roster.AutoSubscribe = true;
      Roster.AutoAllow = AutoSubscriptionHanding.AllowAll;
      Roster.OnRosterItem += OnRosterItem;
      Roster.OnRosterEnd += src => {
        fullyAuthed = true;
        Presence.OnPrimarySessionChange += OnPrimarySessionChange;
        UpdateStatus("");
      };
      Presence.Stream = conn;
      Conference.Stream = conn;

      timer = new Timer(1000) { AutoReset = true };
      timer.Elapsed += UpdateProc;
      timer.Start();
    }

    private void ResetList() {
      var list = new List<Friend>(Friends.Values).Where(u => !u.IsOffline).ToList();
      list.Sort((f1, f2) => {
        if (f1.GameInfo != null && f2.GameInfo != null) {
          int score1 = (int) f1.GameInfo.gameStartTime;
          int score2 = (int) f2.GameInfo.gameStartTime;
          return Math.Sign(score1 - score2);
        } else {
          int score1 = f1.Status.GameStatus.Priority;
          int score2 = f2.Status.GameStatus.Priority;
          if (f1.Status.Show == StatusShow.Away) score1 += 50;
          if (f2.Status.Show == StatusShow.Away) score2 += 50;
          return score1 - score2;
        }
      });
      ChatListUpdated?.Invoke(this, list);
    }

    private void UpdateProc(object src, ElapsedEventArgs args) {
      var list = new List<Friend>(Friends.Values);
      foreach (var friend in list)
        try { App.Current.Dispatcher.Invoke(friend.Update); } catch { timer.Dispose(); }
      try { App.Current.Dispatcher.Invoke(ResetList); } catch { timer.Dispose(); }
    }

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

    #region Event Handlers
    void OnPrimarySessionChange(object sender, jabber.JID bare) {
      if (!Friends.ContainsKey(bare.User)) return;

      var user = Users[bare.User];

      var s = Presence.GetAll(bare);
      if (s.Length == 0 || s[0]?.Status == null)
        Friends[bare.User].IsOffline = true;
      else {
        Friends[bare.User].IsOffline = false;
        Friends[bare.User].Update(s[0]);
      }
      App.Current.Dispatcher.Invoke(ResetList);
      //if (s.Length == 0 || s[0]?.Status == null)
      //  App.Current?.Dispatcher.Invoke((Action<List<ChatUser>>)ResetList, list);
      //else {
      //  App.Current?.Dispatcher.Invoke(() => {
      //    //var friend = new ChatUser(s[0], user, convo);
      //    ////friend.MouseUp += (src, e) => OnChatAdd(friend);
      //    //list.Add(friend);
      //    //ResetList(list);
      //    //Friends[bare.User] = friend;
      //  });
      //}
    }

    void OnChatAdd(Friend friend) {
      if (!OpenChats.Contains(friend.Conversation))
        OpenChats.Add(friend.Conversation);
      if (OpenChats.Count > 8)
        OpenChats.RemoveAt(0);
      friend.Conversation.Open = !friend.Conversation.Open;
    }

    void OnChatOpen(string user) {
      foreach (var chat in OpenChats)
        if (!chat.User.Equals(user)) chat.Open = false;
        else App.Focus(chat.ChatSendBox);
    }

    void OnChatClose(string user) {
      Friends[user].Conversation.Open = false;
      OpenChats.Remove(Friends[user].Conversation);
    }

    void OnReceiveMessage(object sender, Message msg) {
      if (!Users.ContainsKey(msg.From.User)) return;
      string user = msg.From.User;
      if (msg.From.User.Equals(msg.To.User)) {
        return;
      }
      App.Current.Dispatcher.Invoke(() => {
        var convo = Friends[user].Conversation;
        if (!OpenChats.Contains(convo)) {
          OnChatAdd(Friends[user]);
          convo.Open = false;
        }
        if(!convo.Open){
          convo.Unread = true;
        }
        convo.History += $"[{Users[user].Nickname}]: {msg.Body}\n";
      });
    }

    void OnSendMessage(string user, string msg) {
      conn.Message(user + "@pvp.net", msg);
      Friends[user].Conversation.History += $"[{Client.LoginPacket.AllSummonerData.Summoner.Name}]: {msg}\n";
    }

    void OnRosterItem(object sender, jabber.protocol.iq.Item item) {
      if (!Users.ContainsKey(item.JID.User)) {
        Users.Add(item.JID.User, item);

        App.Current.Dispatcher.Invoke(() => {
          var convo = new ChatConversation(item.Nickname, item.JID.User);
          convo.MessageSent += OnSendMessage;
          convo.ChatOpened += OnChatOpen;
          convo.ChatClosed += OnChatClose;
          var friend = new Friend(item, convo);
          friend.MouseUp += (src, e) => OnChatAdd(friend);
          Friends.Add(item.JID.User, friend);
        });
      }
      if (!fullyAuthed)
        OnPrimarySessionChange(sender, item.JID);
    }
    #endregion

    //var convo = new ChatConversation(item.Nickname, item.JID.User);
    //convo.MessageSent += OnSendMessage;
    //    convo.ChatOpened += OnChatOpen;
    //    convo.ChatClosed += OnChatClose;


    /// <summary>
    /// Minimizes any open chat conversations
    /// </summary>
    public void CloseAll() {
      foreach (var item in OpenChats) item.Open = false;
    }

    /// <summary>
    /// Updates the current status string
    /// </summary>
    /// <param name="message">The status message to display</param>
    public void UpdateStatus(string message) {
      var status = new LeagueStatus(Message = message, Status);
      conn.Presence(PresenceType.invisible, status.ToXML(), Show.ToString().ToLower(), 1);
    }

    public void UpdateStatus(GameStatus status) {
      this.Status = status;
      UpdateStatus(Message);
    }

    public void UpdateStatus(StatusShow show) {
      this.Show = show;
      UpdateStatus(Message);
    }

    public Room GetTeambuilderRoom(string groupId, string pass) {
      var room = Conference.GetRoom(new JID(GetChatroomJID(GetObfuscatedChatroomName(groupId, "cp"), true, pass)));
      room.Nickname = Client.LoginPacket.AllSummonerData.Summoner.Name;
      return room;
    }

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

    public enum State {
      Disconnected, Connecting, Connected
    }
  }
}
