using RiotClient.Riot.Platform;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RtmpSharp.Messaging;
using RiotClient.Riot;
using RiotClient.Chat;

namespace RiotClient.Lobbies {
  public class CustomLobby : Lobby {
    public event EventHandler<MemberEventArgs> MemberChangedTeam;
    public event EventHandler<GameDTO> GotGameDTO;
    public event EventHandler<Game> EnteredChampSelect;

    public override int QueueID {
      get { throw new NotImplementedException(); }
    }

    private GameDTO game;
    private IEnumerable<Participant> participants;

    private CustomLobby() { }

    public static async Task<CustomLobby> Create(PracticeGameConfig config) {
      var lobby = new CustomLobby();

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
      var task = invite.Join();
      task.ContinueWith(t => lobby.GotLobbyStatus(t.Result));
      return lobby;
    }

    public void SwitchTeams() {
      RiotServices.GameService.SwitchTeams(game.Id);
    }

    public void SwitchToObserver() {
      RiotServices.GameService.SwitchPlayerToObserver(game.Id);
    }

    public void SwitchToPlayer(int team) {
      RiotServices.GameService.SwitchObserverToPlayer(game.Id, team);
    }

    public async void StartChampSelect() {
      var start = await RiotServices.GameService.StartChampionSelection(game.Id, game.OptimisticLock);
      if (start.InvalidPlayers.Count != 0) {

      }
    }

    public override void CatchUp() {
      base.CatchUp();

      if (loaded) {
        OnGotGameDTO(game);
      }
    }

    public override void EnterQueue() => new NotImplementedException();

    public override int GetIndex(LobbyMember raw) {
      var member = raw as CustomLobbyMember;
      List<Participant> team;

      if (member.Team == 0) team = game.TeamOne;
      else if (member.Team == 1) team = game.TeamTwo;
      else throw new ArgumentException($"Member {raw.Name} not found on either team");

      for (int i = 0; i < team.Count; i++)
        if ((team[i] as PlayerParticipant)?.SummonerId == member.SummonerID)
          return i;
      throw new Exception("WAT");
    }

    public override void Quit() {
      RiotServices.GameService.QuitGame();
      base.Quit();
    }

    internal override bool HandleMessage(MessageReceivedEventArgs args) {
      var game = args.Body as GameDTO;

      if (game != null) {
        GotGameData(game);
        return true;
      }

      return base.HandleMessage(args);
    }

    protected void GotGameData(GameDTO game) {
      this.game = game;

      if (chatID == null) {
        ChatLogin(RiotChat.GetCustomRoom(game.Name, game.Id, game.RoomPassword), game.RoomPassword);
      }

      if (game.GameState.Equals("TEAM_SELECT")) {
        var left = new List<long>(Members.Keys);

        participants = game.TeamOne.Concat(game.TeamTwo);

        foreach (var thing in participants) {
          var player = thing as PlayerParticipant;
          var bot = thing as BotParticipant;
          int team;
          if (game.TeamOne.Contains(thing))
            team = 0;
          else
            team = 1;

          LobbyMember member;
          if (player != null) {
            if (!Members.TryGetValue(player.SummonerId, out member)) {
              member = new CustomLobbyMember(player, team);
              OnMemberJoined(member);
            } else {
              bool diff = ((CustomLobbyMember) member).Team != team;
              (member as CustomLobbyMember).Update(player, team);
              if (diff) OnMemberChangeTeam(member);
            }

            left.Remove(member.SummonerID);
          } else if (bot != null) {
            if (!Members.TryGetValue(bot.Champion.ChampionId, out member)) {
              member = new CustomLobbyMember(bot, team);
              OnMemberJoined(member);
            }

            left.Remove(bot.Champion.ChampionId);
          } else {
            Session.Log(thing);
          }
        }

        foreach (var thing in game.Observers) {
          LobbyMember member;
          if (!Members.TryGetValue(thing.SummonerId, out member)) {
            member = new CustomLobbyMember(thing);
            OnMemberJoined(member);
          } else {
            bool diff = ((CustomLobbyMember) member).Team != 2;
            (member as CustomLobbyMember).Update(thing);
            if (diff) OnMemberChangeTeam(member);
          }
        }

        foreach (var id in left) {
          Members.Remove(id);
        }

        if (!loaded && lobbyStatus != null) {
          OnLoaded();
          OnGotGameDTO(game);
        }
      } else if (game.GameState.Equals("CHAMP_SELECT") || game.GameState.Equals("PRE_CHAMP_SELECT")) {
        OnEnteredChampSelect(game);
        //Close?.Invoke(this, new EventArgs());
        //Client.MainWindow.BeginChampionSelect(game);
      }
    }

    protected override void GotLobbyStatus(LobbyStatus status) {
      base.GotLobbyStatus(status);

      var left = new List<long>(Members.Keys);

      foreach (var raw in status.Members) {
        LobbyMember member;

        if (Members.TryGetValue(raw.SummonerId, out member)) {
          member.Update(raw);
          left.Remove(raw.SummonerId);
        }
      }

      foreach (var id in left) {
        Members.Remove(id);
      }

      if (!loaded && game != null) {
        OnLoaded();
        OnGotGameDTO(game);
      }
    }

    protected virtual void OnMemberChangeTeam(LobbyMember member) {
      MemberChangedTeam?.Invoke(this, new MemberEventArgs(member));
    }

    protected virtual void OnGotGameDTO(GameDTO member) {
      GotGameDTO?.Invoke(this, member);
    }

    protected virtual void OnEnteredChampSelect(GameDTO game) {
      EnteredChampSelect?.Invoke(this, new Game(game));
    }

    public class CustomLobbyMember : LobbyMember {
      private PlayerParticipant player;
      private GameObserver observer;
      private BotParticipant bot;

      public bool IsBot => bot != null;
      public bool IsPlayer => player != null;
      public bool IsObserver => observer != null;
      public int Team { get; private set; }

      public override bool HasInvitePower => IsPlayer ? base.HasInvitePower : false;
      public override long SummonerID {
        get {
          if (IsObserver) return observer.SummonerId;
          else if (IsPlayer) return player.SummonerId;
          else if (IsBot) return bot.Champion.ChampionId;
          return -1;
        }
      }
      public override string Name {
        get {
          if (IsObserver) return observer.SummonerName;
          else if (IsPlayer) return player.SummonerName;
          else if (IsBot) return bot.SummonerName;
          return null;
        }
      }

      public CustomLobbyMember(PlayerParticipant player, int team) : base(null) {
        Update(player, team);
      }

      public CustomLobbyMember(BotParticipant bot, int team) : base(null) {
        Update(bot, team);
      }

      public CustomLobbyMember(GameObserver observer) : base(null) {
        Update(observer);
      }

      public void Update(PlayerParticipant player, int team) {
        this.player = player;
        Team = team;
      }

      public void Update(BotParticipant bot, int team) {
        this.bot = bot;
        Team = team;
      }

      public void Update(GameObserver observer) {
        this.observer = observer;
        Team = 2;
      }
    }
  }
}
