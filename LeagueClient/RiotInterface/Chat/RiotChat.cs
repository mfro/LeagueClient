using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Xml.Linq;
using jabber;
using jabber.client;
using jabber.connection;
using jabber.protocol.client;
using jabber.protocol.iq;
using LeagueClient.ClientUI.Controls;
using LeagueClient.RiotInterface.Riot;

namespace LeagueClient.RiotInterface.Chat {
  public class RiotChat {
    public State ChatState { get; private set; }
    public BindingList<ChatConversation> OpenChats { get; private set; }
    public BindingList<Friend> FriendList { get; private set; }

    private JabberClient conn;
    private RosterManager Roster = new RosterManager();
    private PresenceManager Presence = new PresenceManager();
    private ConferenceManager Conference = new ConferenceManager();
    private Dictionary<string, Item> Users = new Dictionary<string, Item>();
    private Dictionary<string, Friend> Friends = new Dictionary<string, Friend>();
    private bool fullyAuthed;
    private Timer timer;

    public GameStatus Status { get; private set; }
    public StatusShow Show { get; private set; }
    public string Message { get; private set; }

    public RiotChat(string user, string pass) {
      FriendList = new BindingList<Friend>();
      OpenChats = new BindingList<ChatConversation>();
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

    public void UpdateStatus(GameStatus status, StatusShow show) {
      this.Status = status;
      this.Show = show;
      UpdateStatus(Message);
    }

    private void ResetList(List<Friend> list) {
      list.Sort((f1, f2) => {
        if (f1.GameInfo != null && f2.GameInfo != null) {
          return Math.Sign(f2.GameInfo.gameStartTime - f1.GameInfo.gameStartTime);
        } else {
          int score1 = f1.Status.GameStatus.Priority;
          int score2 = f2.Status.GameStatus.Priority;
          if (f1.Status.Show == StatusShow.Away) score1 += 50;
          if (f2.Status.Show == StatusShow.Away) score2 += 50;
          return score1 - score2;
        }
      });
      FriendList.Clear();
      foreach (var item in list) FriendList.Add(item);
    }

    private void UpdateProc(object src, ElapsedEventArgs args) {
      if (App.Current == null) timer.Stop();
      foreach (var friend in new List<Friend>(Friends.Values))
        App.Current.Dispatcher.Invoke(friend.Update);
    }

    #region Event Handlers
    void OnPrimarySessionChange(object sender, jabber.JID bare) {
      if (bare.User.Equals(conn.JID.User)) return;
      var list = new List<Friend>(FriendList);

      var user = Users[bare.User];

      if (Friends.ContainsKey(bare.User))
        list.Remove(Friends[bare.User]);
      var s = Presence.GetAll(bare);
      if (s.Length == 0)
        App.Current.Dispatcher.Invoke((Action<List<Friend>>)ResetList, list);
      else {
        App.Current.Dispatcher.Invoke(() => {
          var convo = new ChatConversation(user.Nickname, user.JID.User);
          convo.MessageSent += OnSendMessage;
          convo.ChatOpened += OnChatOpen;
          convo.ChatClosed += OnChatClose;

          var friend = new Friend(s[0], user, convo);
          friend.MouseUp += (src, e) => OnChatAdd(friend);
          list.Add(friend);
          ResetList(list);
          Friends[bare.User] = friend;
        });
      }
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
    }

    void OnChatClose(string user) {
      App.Current.Dispatcher.Invoke(() => OpenChats.Remove(Friends[user].Conversation));
    }

    void OnReceiveMessage(object sender, Message msg) {
      string user = msg.From.User;
      App.Current.Dispatcher.Invoke(() => {
        var convo = Friends[user].Conversation;
        if (!OpenChats.Contains(convo)) {
          OnChatAdd(Friends[user]);
          convo.Open = false;
        }
        if(!convo.Open){
          convo.Unread = true;
        }
        convo.History +=
        string.Format("[{0}]: {1}\n", Users[user].Nickname, msg.Body);
      });
    }

    void OnSendMessage(string user, string msg) {
      conn.Message(user + "@pvp.net", msg);
      Friends[user].Conversation.History +=
        string.Format("[{0}]: {1}\n", Client.LoginPacket.AllSummonerData.Summoner.Name, msg);
    }

    void OnRosterItem(object sender, jabber.protocol.iq.Item item) {
      if (!Users.ContainsKey(item.JID.User))
        Users.Add(item.JID.User, item);
      if (!fullyAuthed)
        OnPrimarySessionChange(sender, item.JID);
    }
    #endregion

    public void UpdateStatus(string message) {
      var status = new LeagueStatus(Message = message, Status);
      conn.Presence(PresenceType.available, status.ToXML(), Show.ToString().ToLower(), 1);
    }

    public void Disconnect() {
      timer.Stop();
      conn.Close();
    }

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
