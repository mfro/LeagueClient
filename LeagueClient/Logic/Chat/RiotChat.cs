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
using agsXMPP;
using agsXMPP.protocol.client;
using agsXMPP.protocol.iq.roster;
using agsXMPP.protocol.x.muc;
using System.IO;
using LeagueClient.UI.Main.Friends;

namespace LeagueClient.Logic.Chat {
  public sealed class RiotChat : IDisposable {
    public event EventHandler Tick;
    private static readonly List<ChatStatus> DndStatuses = new List<ChatStatus> {
      ChatStatus.championSelect, ChatStatus.tutorial, ChatStatus.inGame, ChatStatus.inQueue, ChatStatus.spectating
    };

    public event EventHandler<StatusUpdatedEventArgs> StatusUpdated;
    public event EventHandler<Presence> PresenceRecieved;
    public event EventHandler<Message> MessageReceived;

    public Dictionary<string, ChatFriend> Friends { get; } = new Dictionary<string, ChatFriend>();
    public BindingList<ChatConversation> OpenChats { get; } = new BindingList<ChatConversation>();
    public BindingList<ChatFriend> FriendList { get; } = new BindingList<ChatFriend>();

    public ChatStatus Status {
      get { return status; }
      set {
        status = value;
        SendPresence();
      }
    }
    public string StatusMessage {
      get { return Client.Session.Settings.StatusMessage; }
      set {
        Client.Session.Settings.StatusMessage = value;
        SendPresence();
      }
    }
    public ChatMode ChatMode {
      get { return chatMode; }
      set {
        chatMode = value;
        SendPresence();
      }
    }

    private XmppClientConnection xmpp;
    private PresenceManager presence;
    private RosterManager roster;
    private MucManager lobby;
    private Timer timer;
    private bool connected;

    private ChatStatus status = ChatStatus.outOfGame;
    private ChatMode chatMode = ChatMode.Chat;

    public RiotChat() {
      xmpp = new XmppClientConnection {
        Server = "pvp.net",
        Port = 5223,
        ConnectServer = Client.Region.ChatServer,
        AutoResolveConnectServer = false,
        Resource = "xiff",
        UseSSL = true,
        KeepAliveInterval = 10,
        KeepAlive = true,
        UseCompression = true,
        AutoPresence = true,
        Status = new LeagueStatus(StatusMessage, Status).ToXML(),
        Show = ShowType.chat,
        Priority = 0
      };
      xmpp.OnMessage += Xmpp_OnMessage;
      xmpp.OnAuthError += (o, e) => Client.Log(e);
      xmpp.OnError += (o, e) => Client.Log(e);
      xmpp.OnLogin += o => Client.Log("Connected to chat server");
      xmpp.Open(Client.Session.UserSession.AccountSummary.Username, "AIR_" + Client.Session.UserSession.Password);

      presence = new PresenceManager(xmpp);
      roster = new RosterManager(xmpp);
      lobby = new MucManager(xmpp);

      xmpp.OnRosterEnd += o => connected = true;
      xmpp.OnRosterItem += Xmpp_OnRosterItem;
      xmpp.OnPresence += Xmpp_OnPresence;

      timer = new Timer(1000) { AutoReset = true };
      timer.Elapsed += UpdateProc;
      timer.Start();
    }

    #region Event Handlers
    private void Xmpp_OnPresence(object sender, Presence pres) {
      if (Friends.ContainsKey(pres.From.User)) {
        Friends[pres.From.User].UpdatePresence(pres);
        Application.Current.Dispatcher.MyInvoke(ResetList, false);
      } else if (!pres.From.User.Equals(xmpp.MyJID.User)) { }
      PresenceRecieved?.Invoke(this, pres);
    }

    private void Xmpp_OnRosterItem(object sender, RosterItem item) {
      if (!Friends.ContainsKey(item.Jid.User))
        Friends.Add(item.Jid.User, new ChatFriend(item));
    }

    private void Xmpp_OnMessage(object sender, Message msg) {
      if (Friends.ContainsKey(msg.From.User) && !msg.From.User.Equals(msg.To.User)) {
        Friends[msg.From.User].ReceiveMessage(msg.Body);
      } else { }
      MessageReceived?.Invoke(this, msg);
    }
    #endregion

    #region Private Methods
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
      try { Application.Current.Dispatcher.MyInvoke(ResetList, false); } catch { Dispose(); }
      Tick?.Invoke(this, new EventArgs());
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
      return (Type + "~" + obfuscatedName).ToLower();
    }

    private static string GetChatroomJID(string obfuscatedName, bool isPublic, string pass = null) {
      if (!isPublic) return obfuscatedName + "@sec.pvp.net";
      else if (pass == null) return obfuscatedName + "@lvl.pvp.net";
      else return obfuscatedName + "@conference.pvp.net";
    }

    public static Jid GetTeambuilderRoom(string groupId) {
      return new Jid(GetChatroomJID(GetObfuscatedChatroomName(groupId, "cp"), true, ""));
    }

    public static Jid GetCustomRoom(string roomname, double roomId, string pass) {
      return new Jid(GetChatroomJID(GetObfuscatedChatroomName(roomname.ToLower() + (int) roomId, "ap"), false, pass));
    }

    public static Jid GetLobbyRoom(string inviteId, string pass) {
      return new Jid(GetChatroomJID(GetObfuscatedChatroomName(inviteId.ToLower(), "ag"), false, pass));
    }

    public void JoinRoom(Jid jid, string pass = null) {
      lobby.AcceptDefaultConfiguration(jid);
      if (pass == null)
        lobby.JoinRoom(jid, Client.Session.LoginPacket.AllSummonerData.Summoner.Name, false);
      else
        lobby.JoinRoom(jid, Client.Session.LoginPacket.AllSummonerData.Summoner.Name, pass, false);
    }

    public void LeaveRoom(Jid jid) {
      lobby.LeaveRoom(jid, Client.Session.LoginPacket.AllSummonerData.Summoner.Name);
    }
    #endregion

    public void SendPresence() {
      var status = new LeagueStatus(StatusMessage, Status);

      ShowType computed;
      if (DndStatuses.Contains(Status))
        computed = ShowType.dnd;
      else if (ChatMode == ChatMode.Away)
        computed = ShowType.away;
      else
        computed = ShowType.chat;

      var args = new StatusUpdatedEventArgs(status, ChatMode == ChatMode.Invisible ? PresenceType.invisible : PresenceType.available, computed);
      StatusUpdated?.Invoke(this, args);

      if (!connected) return;
      xmpp.Send(new Presence(computed, status.ToXML(), 100) { Type = args.PresenceType });
    }

    #region Other Public Methods
    /// <summary>
    /// Parses the internal summoner ID of a friend from the JID used in the XMPP chat service
    /// </summary>
    /// <param name="user">The summoner's JID</param>
    /// <returns>The summoner ID</returns>
    public static long GetSummonerId(Jid user) {
      long d;
      if (long.TryParse(user.User.Substring(3), out d)) return d;
      else throw new FormatException(user.User + " is not correctly formatted");
    }

    public static string GetUser(long summonerId) {
      return "sum" + summonerId;
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
    public void SendMessage(Jid user, string message, MessageType type = MessageType.chat) {
      xmpp.Send(new Message(user, MessageType.chat, message) { Type = type });
    }

    public void ForceUpdate() {
      Application.Current.Dispatcher.MyInvoke(ResetList, true);
    }

    public void Dispose() {
      timer.Dispose();
      xmpp.Close();
      xmpp.SocketDisconnect();
    }
    #endregion
  }

  public enum ChatMode {
    Chat, Away, /*Offline, */Invisible
  }

  public class StatusUpdatedEventArgs : EventArgs {
    public LeagueStatus Status { get; set; }
    public PresenceType PresenceType { get; set; }
    public ShowType Show { get; set; }

    public StatusUpdatedEventArgs(LeagueStatus status, PresenceType presenceType, ShowType show) {
      Status = status;
      PresenceType = presenceType;
      Show = show;
    }
  }
}