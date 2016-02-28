using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RtmpSharp.Messaging;
using RiotClient.Riot.Platform;
using RiotClient.Chat;

namespace RiotClient.Lobbies {
  public class QueueLobby : Lobby {
    public event EventHandler<QueueEventArgs> QueueEntered;
    public event EventHandler QueueLeft;

    public override GroupChat ChatLobby { get; protected set; }

    public int QueueID { get; }
    public bool IsCaptain => lobbyStatus.Owner.SummonerId == Session.Current.Account.SummonerID;
    public List<QueueLobbyMember> Members { get; } = new List<QueueLobbyMember>();
    public Dictionary<long, LobbyInvitee> Invitees { get; } = new Dictionary<long, LobbyInvitee>();

    protected bool canInvite;
    protected LobbyStatus lobbyStatus;
    protected Queue queue;

    protected QueueLobby(int queueId) {
      QueueID = queueId;
    }

    public static QueueLobby CreateLobby(int queueId) {
      var lobby = new QueueLobby(queueId);
      Session.Current.CurrentLobby = lobby;
      var task = RiotServices.GameInvitationService.CreateArrangedTeamLobby(queueId);
      task.ContinueWith(t => lobby.GotLobbyStatus(t.Result));
      return lobby;
    }

    public static QueueLobby Join(Invitation invite, int queueId) {
      var lobby = new QueueLobby(queueId);
      Session.Current.CurrentLobby = lobby;
      var task = invite.Join();
      task.ContinueWith(t => lobby.GotLobbyStatus(t.Result));
      return lobby;
    }

    public virtual void CatchUp() {
      foreach (var item in Members) {
        OnMemberJoined(item);
      }

      foreach (var item in Invitees.Values) {
        OnMemberJoined(item);
      }

      if (loaded) {
        loaded = false;
        OnLoaded();
      }
    }

    public virtual void Invite(long summonerId) {
      if (!canInvite && !IsCaptain) return;

      RiotServices.GameInvitationService.Invite(summonerId);
    }

    public virtual void Kick(QueueLobbyMember member) {
      if (!IsCaptain) return;

      RiotServices.GameInvitationService.Kick(member.SummonerID);

      OnMemberLeft(member);
    }

    public virtual void GiveInvitePowers(QueueLobbyMember member, bool canInvite) {
      if (!IsCaptain) return;

      if (canInvite) RiotServices.GameInvitationService.RevokeInvitePrivileges(member.SummonerID);
      else RiotServices.GameInvitationService.GrantInvitePrivileges(member.SummonerID);

      var raw = lobbyStatus.Members.FirstOrDefault(m => m.SummonerId == member.SummonerID);
      raw.HasInvitePower = canInvite;
      member.Update(raw);
    }

    public virtual async void StartQueue() {
      if (!IsCaptain) return;

      var mmp = new MatchMakerParams {
        InvitationId = lobbyStatus.InvitationID,
        QueueIds = new[] { QueueID },
        Team = Members.Select(m => (int) m.SummonerID).ToList()
      };

      var search = await RiotServices.MatchmakerService.AttachTeamToQueue(mmp);
      OnQueueEntered(search);
    }

    public virtual void CancelQueue() {
      queue.Cancel();
    }

    public override void Dispose() {
      if (queue != null) CancelQueue();
      RiotServices.GameInvitationService.Leave();
      ChatLobby.Dispose();
      OnLeftLobby();
      //TODO Queue
    }

    internal override bool HandleMessage(MessageReceivedEventArgs args) {
      var lobby = args.Body as LobbyStatus;
      var invite = args.Body as InvitePrivileges;

      if (lobby != null) {
        GotLobbyStatus(lobby);
        return true;
      } else if (invite != null) {
        canInvite = invite.canInvite;
        return true;
      }

      return false;
    }

    protected virtual void GotLobbyStatus(LobbyStatus status) {
      lobbyStatus = status;

      if (ChatLobby == null) {
        ChatLobby = new GroupChat(RiotChat.GetLobbyRoom(status.InvitationID, status.ChatKey), status.ChatKey);
      }

      var todo = status.Members.ToDictionary(m => m.SummonerId);

      foreach (var member in Members) {
        Member raw;
        if (todo.TryGetValue(member.SummonerID, out raw)) {
          member.Update(raw);
          todo.Remove(member.SummonerID);
        } else {
          Members.Remove(member);
          OnMemberLeft(member);
        }
      }

      foreach (var raw in todo.Values) {
        var member = new QueueLobbyMember(raw, this);
        Members.Add(member);
        OnMemberJoined(member);
      }

      foreach (var raw in status.InvitedPlayers) {
        if (!Invitees.ContainsKey(raw.SummonerId)) {
          var invitee = new LobbyInvitee(raw, this);
          Invitees.Add(invitee.SummonerID, invitee);
          OnMemberJoined(invitee);
        }
      }
      if (!loaded) OnLoaded();
    }

    protected virtual void OnQueueEntered(SearchingForMatchNotification searching) {
      try {
        queue = Queue.Create(searching);
        queue.QueueCancelled += (s, e) => OnQueueLeft();
        QueueEntered?.Invoke(this, new QueueEventArgs(queue));
      } catch (Exception x) {
        //TODO Queue Dodge and the like
        Session.ThrowException(x, "Trying to enter queue");
        Dispose();
      }
    }

    protected virtual void OnQueueLeft() {
      queue = null;
      QueueLeft?.Invoke(this, new EventArgs());
    }
  }

  public class QueueLobbyMember : LobbyMember {
    protected Member member;

    public override string Name => member.SummonerName;
    public override long SummonerID => member.SummonerId;
    public virtual bool HasInvitePower => member.HasInvitePower;

    internal QueueLobbyMember(Member member, QueueLobby lobby) : base(lobby) {
      Update(member);
    }

    public void GiveInvitePowers(bool canInvite) {
      ((QueueLobby) lobby).GiveInvitePowers(this, canInvite);
    }

    public void Kick() {
      ((QueueLobby) lobby).Kick(this);
    }

    internal void Update(Member member) {
      this.member = member;

      OnChange();
    }
  }

  public class LobbyInvitee : LobbyMember {
    protected Invitee invitee;

    public string State => invitee.InviteeState;

    public override string Name => invitee.SummonerName;
    public override long SummonerID => invitee.SummonerId;

    internal LobbyInvitee(Invitee invitee, Lobby lobby) : base(lobby) {
      this.invitee = invitee;
    }

    internal void Update(Invitee invitee) {
      this.invitee = invitee;

      OnChange();
    }
  }

  public class QueueEventArgs : EventArgs {
    public Queue Queue { get; }
    public QueueEventArgs(Queue queue) {
      Queue = queue;
    }
  }

  public class InviteeEventArgs : EventArgs {
    public LobbyInvitee Invitee { get; }
    public InviteeEventArgs(LobbyInvitee invitee) {
      Invitee = invitee;
    }
  }
}
