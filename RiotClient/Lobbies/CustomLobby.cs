using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RtmpSharp.Messaging;
using RiotClient.Riot.Platform;
using RiotClient.Chat;

namespace RiotClient.Lobbies {
  public class CustomLobby : Lobby {
    public event EventHandler<MemberEventArgs> MemberChangedTeam;
    public event EventHandler<GameLobby> GameStarted;

    public EventHandler Updated;

    public GameDTO Data { get; protected set; }
    public List<CustomLobbyMember> TeamOne { get; } = new List<CustomLobbyMember>();
    public List<CustomLobbyMember> TeamTwo { get; } = new List<CustomLobbyMember>();
    public List<CustomLobbyMember> Observers { get; } = new List<CustomLobbyMember>();
    public IEnumerable<CustomLobbyMember> AllMembers => TeamOne.Concat(TeamTwo).Concat(Observers);

    public CustomLobbyMember Me => AllMembers.SingleOrDefault(m => m.IsMe);
    public bool IsCaptain => (lobbyStatus?.Owner?.SummonerId ?? Me.SummonerID) == Session.Current.Account.SummonerID;
    protected bool canInvite;

    public override GroupChat ChatLobby { get; protected set; }

    public Dictionary<long, LobbyInvitee> Invitees { get; } = new Dictionary<long, LobbyInvitee>();
    protected Dictionary<long, Member> members = new Dictionary<long, Member>();
    protected LobbyStatus lobbyStatus;

    protected CustomLobby() { }

    public static async Task<CustomLobby> CreateLobby(PracticeGameConfig config) {
      var lobby = new CustomLobby();
      Session.Current.CurrentLobby = lobby;

      GameDTO game = await RiotServices.GameService.CreatePracticeGame(config);

      if (game?.Name == null) {
        throw new Exception("Invalid name");
      } else {
        lobby.GotGameData(game);
        return lobby;
      }
    }

    internal static CustomLobby Join(Invitation invite) {
      var lobby = new CustomLobby();
      Session.Current.CurrentLobby = lobby;
      var task = invite.Join();
      task.ContinueWith(t => lobby.GotLobbyStatus(t.Result));
      return lobby;
    }

    public virtual void CatchUp() {
      foreach (var item in AllMembers) {
        OnMemberJoined(item);
      }

      foreach (var item in Invitees.Values) {
        OnMemberJoined(item);
      }

      if (loaded) {
        loaded = false;
        OnLoaded();
        OnUpdate(Data);
      }
    }

    public virtual void Kick(CustomLobbyMember member) {
      if (!IsCaptain) return;

      RiotServices.GameInvitationService.Kick(member.SummonerID);

      OnMemberLeft(member);
    }

    public virtual void Invite(long summonerId) {
      if (!canInvite && !IsCaptain) return;

      RiotServices.GameInvitationService.Invite(summonerId);
    }

    public virtual bool HasInvitePowers(CustomLobbyMember member) {
      return members[member.SummonerID].HasInvitePower;
    }

    public virtual void GiveInvitePowers(CustomLobbyMember member, bool canInvite) {
      if (!IsCaptain) return;

      if (canInvite) RiotServices.GameInvitationService.RevokeInvitePrivileges(member.SummonerID);
      else RiotServices.GameInvitationService.GrantInvitePrivileges(member.SummonerID);

      var raw = lobbyStatus.Members.FirstOrDefault(m => m.SummonerId == member.SummonerID);
      raw.HasInvitePower = canInvite;
    }

    public virtual void SwitchTeams() {
      if (Observers.Contains(Me)) return;
      RiotServices.GameService.SwitchTeams(Data.Id);
    }

    public virtual void SwitchToObserver() {
      if (Observers.Contains(Me)) return;
      RiotServices.GameService.SwitchPlayerToObserver(Data.Id);
    }

    public virtual void SwitchToPlayer(int team) {
      if (!Observers.Contains(Me)) return;
      RiotServices.GameService.SwitchObserverToPlayer(Data.Id, team);
    }

    public virtual async void StartChampSelect() {
      var start = await RiotServices.GameService.StartChampionSelection(Data.Id, Data.OptimisticLock);
      if (start.InvalidPlayers.Count != 0) {

      }
    }

    public override void Dispose() {
      ChatLobby.Dispose();
    }

    internal override bool HandleMessage(MessageReceivedEventArgs args) {
      var invite = args.Body as InvitePrivileges;
      var lobby = args.Body as LobbyStatus;
      var game = args.Body as GameDTO;

      if (game != null) {
        GotGameData(game);
        return true;
      } else if (lobby != null) {
        GotLobbyStatus(lobby);
        return true;
      } else if (invite != null) {
        canInvite = invite.canInvite;
        return true;
      }

      return false;
    }

    protected virtual void GotGameData(GameDTO game) {
      Data = game;

      if (ChatLobby == null) {
        ChatLobby = new GroupChat(RiotChat.GetCustomRoom(game.RoomName, game.Id, game.RoomPassword), game.RoomPassword);
      }

      var participants = game.TeamOne.Concat(game.TeamTwo);
      var left = new List<CustomLobbyMember>(AllMembers);

      foreach (var thing in participants) {
        var player = thing as PlayerParticipant;
        var bot = thing as BotParticipant;
        int team;
        if (game.TeamOne.Contains(thing)) team = 0;
        else team = 1;

        CustomLobbyMember now;

        if (player != null) {
          now = new CustomLobbyMember(player, team, this);
        } else if (bot != null) {
          now = new CustomLobbyMember(bot, team, this);
        } else throw new Exception("Unknown participant " + thing);

        CustomLobbyMember old = AllMembers.SingleOrDefault(m => m.SummonerID == now.SummonerID);
        if (old != null) {
          TeamOne.Remove(old);
          TeamTwo.Remove(old);
          Observers.Remove(old);

          bool diff = old.Team != now.Team;
          old.Update(now);
          if (team == 0) TeamOne.Add(old);
          else TeamTwo.Add(old);

          if (diff) OnMemberChangeTeam(old);
          left.Remove(old);
        } else {
          if (team == 0) TeamOne.Add(now);
          else TeamTwo.Add(now);

          OnMemberJoined(now);
        }
      }

      foreach (var thing in game.Observers) {
        var now = new CustomLobbyMember(thing, this);

        CustomLobbyMember old = AllMembers.SingleOrDefault(m => m.SummonerID == thing.SummonerId);

        if (old != null) {
          TeamOne.Remove(old);
          TeamTwo.Remove(old);
          Observers.Remove(old);

          bool diff = old.Team != now.Team;
          old.Update(now);
          Observers.Add(old);

          if (diff) OnMemberChangeTeam(old);
          left.Remove(old);
        } else {
          Observers.Add(now);

          OnMemberJoined(now);
        }
      }

      foreach (var member in left) {
        TeamOne.Remove(member);
        TeamTwo.Remove(member);
        OnMemberLeft(member);
      }

      if (game.GameState.Contains("CHAMP_SELECT")) {
        var champSelect = GameLobby.EnterChampSelect(game);
        OnGameStart(champSelect);
      }

      OnUpdate(game);
      if (!loaded) OnLoaded();
    }

    protected virtual void GotLobbyStatus(LobbyStatus status) {
      foreach (var raw in status.Members) {
        members[raw.SummonerId] = raw;
      }

      foreach (var raw in status.InvitedPlayers) {
        if (!Invitees.ContainsKey(raw.SummonerId)) {
          var invitee = new LobbyInvitee(raw, this);
          Invitees.Add(invitee.SummonerID, invitee);
          OnMemberJoined(invitee);
        }
      }
    }

    protected virtual void OnMemberChangeTeam(GameMember member) {
      MemberChangedTeam?.Invoke(this, new MemberEventArgs(member));
    }

    protected virtual void OnGameStart(GameLobby champSelect) {
      GameStarted?.Invoke(this, champSelect);
    }

    protected virtual void OnUpdate(GameDTO game) {
      Updated?.Invoke(this, new EventArgs());
    }
  }

  public class CustomLobbyMember : GameMember {
    protected GameObserver observer;

    public bool IsObserver => observer != null;

    public override string Name {
      get {
        if (IsBot) return bot.SummonerName;
        else if (IsPlayer) return player.SummonerName;
        else if (IsObserver) return observer.SummonerName;
        return null;
      }
    }
    public override long SummonerID {
      get {
        if (IsBot) return bot.Champion.ChampionId;
        else if (IsPlayer) return player.SummonerId;
        else if (IsObserver) return observer.SummonerId;
        return -1;
      }
    }
    public bool HasInvitePowers => ((CustomLobby) lobby).HasInvitePowers(this);

    internal CustomLobbyMember(PlayerParticipant player, int team, CustomLobby lobby) : base(player, team, lobby) { }
    internal CustomLobbyMember(BotParticipant bot, int team, CustomLobby lobby) : base(bot, team, lobby) { }

    internal CustomLobbyMember(GameObserver observer, CustomLobby lobby) : base((BotParticipant) null, 2, lobby) {
      this.observer = observer;
      Team = 2;
    }

    public void Kick() {
      ((CustomLobby) lobby).Kick(this);
    }

    public void GiveInvitePowers(bool canInvite) {
      ((CustomLobby) lobby).GiveInvitePowers(this, canInvite);
    }

    internal void Update(CustomLobbyMember now) {
      Team = now.Team;

      if (now.IsBot && IsBot) bot = now.bot;
      else if (now.IsPlayer && IsPlayer) player = now.player;
      else if (now.IsObserver && IsObserver) observer = now.observer;
      else throw new Exception("Cant update");

      OnChange();
    }
  }
}
