using agsXMPP;
using agsXMPP.protocol.client;
using agsXMPP.protocol.x.muc;
using RiotClient.Chat;
using RiotClient.Riot;
using RiotClient.Riot.Platform;
using RtmpSharp.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RiotClient.Lobbies {
  public abstract class Lobby {
    public event EventHandler<InviteeEventArgs> PlayerInvited;

    public event EventHandler<MemberEventArgs> MemberJoined;
    public event EventHandler<MemberEventArgs> MemberLeft;

    public event EventHandler<QueueEventArgs> QueueEntered;
    public event EventHandler QueueLeft;

    public event EventHandler LeftLobby;
    public event EventHandler Loaded;

    public event EventHandler<ChatMessageEventArgs> ChatMessage;
    public event EventHandler<ChatStatusEventArgs> ChatStatus;

    protected LobbyStatus lobbyStatus;
    protected Queue queue;

    protected bool loaded;
    protected Jid chatID;

    public Dictionary<long, LobbyInvitee> Invitees { get; } = new Dictionary<long, LobbyInvitee>();
    public Dictionary<long, LobbyMember> Members { get; } = new Dictionary<long, LobbyMember>();
    public virtual LobbyMember Me => Members[Session.Current.Account.SummonerID];

    public Dictionary<string, Item> Users { get; } = new Dictionary<string, Item>();

    public bool IsCaptain => lobbyStatus.Owner.SummonerId == Session.Current.Account.SummonerID;
    public abstract int QueueID { get; }

    public Lobby() {
      Session.Current.CurrentLobby = this;
    }

    public virtual void CatchUp() {
      foreach (var member in Members) {
        OnMemberJoined(member.Value);
      }

      foreach (var invitee in Invitees) {
        OnPlayerInvited(invitee.Value);
      }

      if (loaded) OnLoaded();
    }

    internal virtual bool HandleMessage(MessageReceivedEventArgs args) {
      var lobby = args.Body as LobbyStatus;
      var invite = args.Body as InvitePrivileges;

      if (lobby != null) {
        GotLobbyStatus(lobby);
        return true;
      } else if (invite != null) {
        //Session.Instance.CanInviteFriends = invite.canInvite;
        return true;
      }

      return false;
    }

    public virtual void Quit() {
      RiotServices.GameInvitationService.Leave();
      LeftLobby?.Invoke(this, new EventArgs());
    }

    public virtual void Kick(LobbyMember member) {
      if (!IsCaptain) return;

      RiotServices.GameInvitationService.Kick(member.SummonerID);

      OnMemberLeft(member);
    }

    public virtual void SendMessage(string message) {
      Session.Current.ChatManager.SendMessage(chatID, message, MessageType.groupchat);
    }

    public virtual void GiveInvitePowers(LobbyMember member, bool canInvite) {
      if (!IsCaptain) return;

      if (canInvite) RiotServices.GameInvitationService.RevokeInvitePrivileges(member.SummonerID);
      else RiotServices.GameInvitationService.GrantInvitePrivileges(member.SummonerID);

      var raw = lobbyStatus.Members.FirstOrDefault(m => m.SummonerId == member.SummonerID);
      raw.HasInvitePower = canInvite;
      member.Update(raw);
    }

    public virtual void Invite(long summonerId) {
      RiotServices.GameInvitationService.Invite(summonerId);
    }

    public virtual void LeaveQueue() {
      queue.Leave();
    }

    public virtual int GetIndex(LobbyMember member) {
      for (int i = 0; i < lobbyStatus.Members.Length; i++)
        if (member.SummonerID == lobbyStatus.Members[i].SummonerId)
          return i;
      throw new ArgumentException($"Member {member.Name} not found in lobby");
    }

    public abstract void EnterQueue();

    protected void ChatLogin(Jid id, string pass) {
      chatID = id;
      Session.Current.ChatManager.PresenceRecieved += ChatManager_PresenceRecieved;
      Session.Current.ChatManager.MessageReceived += ChatManager_MessageReceived;
      Session.Current.ChatManager.JoinRoom(chatID, pass);
      Session.Current.ChatManager.SendPresence();
    }

    protected virtual void GotLobbyStatus(LobbyStatus status) {
      lobbyStatus = status;

      foreach (var raw in status.InvitedPlayers) {
        if (!Invitees.ContainsKey(raw.SummonerId)) {
          var invitee = new LobbyInvitee(raw);
          OnPlayerInvited(invitee);
        }
      }
    }

    protected virtual void OnPlayerInvited(LobbyInvitee invitee) {
      if (!Invitees.ContainsKey(invitee.SummonerID))
        Invitees[invitee.SummonerID] = invitee;
      PlayerInvited?.Invoke(this, new InviteeEventArgs(invitee));
    }

    protected virtual void OnMemberJoined(LobbyMember member) {
      if (!Members.ContainsKey(member.SummonerID))
        Members[member.SummonerID] = member;
      MemberJoined?.Invoke(this, new MemberEventArgs(member));
    }

    protected virtual void OnMemberLeft(LobbyMember member) {
      Members.Remove(member.SummonerID);
      MemberLeft?.Invoke(this, new MemberEventArgs(member));
    }

    protected virtual void OnQueueEntered(SearchingForMatchNotification searching) {
      try {
        queue = Queue.Create(searching);
        queue.QueueCancelled += (s, e) => OnQueueLeft();
        QueueEntered?.Invoke(this, new QueueEventArgs(queue));
      } catch (Exception x) {
        Session.ThrowException(x, "Trying to enter queue");
        Quit();
      }
    }

    protected virtual void OnQueueLeft() {
      queue = null;
      QueueLeft?.Invoke(this, new EventArgs());
    }

    protected virtual void OnLeftLobby() {
      loaded = true;
      LeftLobby?.Invoke(this, new EventArgs());
    }

    protected virtual void OnLoaded() {
      loaded = true;
      Loaded?.Invoke(this, new EventArgs());
    }

    protected virtual void OnChatMessage(string user, string body) {
      ChatMessage?.Invoke(this, new ChatMessageEventArgs(user, body));
    }

    protected virtual void OnChatStatus(string user, bool status) {
      ChatStatus?.Invoke(this, new ChatStatusEventArgs(user, status));
    }

    private void ChatManager_MessageReceived(object sender, Message e) {
      if (!e.From.User.Equals(chatID.User) || e.Type != MessageType.groupchat) return;

      ChatMessage?.Invoke(this, new ChatMessageEventArgs(e.From.Resource, e.Body));
    }

    private void ChatManager_PresenceRecieved(object sender, Presence e) {
      if (!e.From.User.Equals(chatID.User)) return;
      if (e.Status == null && e.Type == PresenceType.available) {
        Session.Current.ChatManager.SendPresence();
        return;
      }

      var user = e.MucUser.Item;
      if (e.Type == PresenceType.available) {
        if (!Users.ContainsKey(user.Jid.User)) {
          OnChatStatus(e.From.Resource, true);
          Users.Add(user.Jid.User, user);
        }
      } else {
        if (Users.ContainsKey(user.Jid.User))
          OnChatStatus(e.From.Resource, false);
        Users.Remove(user.Jid.User);
      }
    }
  }

  public abstract class LobbyMember {
    public event EventHandler Changed;

    private Member member;
    private Lobby lobby;

    public virtual string Name => member.SummonerName;
    public virtual long SummonerID => member.SummonerId;
    public virtual bool HasInvitePower => member.HasInvitePower;
    public virtual bool IsMe => SummonerID == Session.Current.Account.SummonerID;

    public LobbyMember(Member member) {
      Update(member);
    }

    public void Update(Member member) {
      this.member = member;

      Changed?.Invoke(this, new EventArgs());
    }

    public void GiveInvitePowers(bool canInvite) {
      lobby.GiveInvitePowers(this, canInvite);
    }

    public void Kick() {
      lobby.Kick(this);
    }

    protected void OnChange() {
      Changed?.Invoke(this, new EventArgs());
    }
  }

  public class LobbyInvitee {
    public event EventHandler Changed;

    private Invitee invitee;

    public string State => invitee.InviteeState;

    public string Name => invitee.SummonerName;
    public long SummonerID => invitee.SummonerId;

    public LobbyInvitee(Invitee invitee) {
      this.invitee = invitee;
    }

    public void Update(Invitee invitee) {
      this.invitee = invitee;

      Changed?.Invoke(this, new EventArgs());
    }
  }

  public class MemberEventArgs : EventArgs {
    public LobbyMember Member { get; }
    public MemberEventArgs(LobbyMember member) {
      Member = member;
    }
  }

  public class InviteeEventArgs : EventArgs {
    public LobbyInvitee Invitee { get; }
    public InviteeEventArgs(LobbyInvitee invitee) {
      Invitee = invitee;
    }
  }

  public class ChatMessageEventArgs {
    public string Message { get; }
    public string From { get; }

    public ChatMessageEventArgs(string from, string message) {
      Message = message;
      From = from;
    }
  }

  public class ChatStatusEventArgs {
    public string From { get; }
    public bool IsOnline { get; }

    public ChatStatusEventArgs(string from, bool status) {
      From = from;
      IsOnline = status;
    }
  }

  public class QueueEventArgs : EventArgs {
    public Queue Queue { get; }
    public QueueEventArgs(Queue queue) {
      Queue = queue;
    }
  }
}
