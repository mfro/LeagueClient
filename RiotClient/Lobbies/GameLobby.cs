using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RtmpSharp.Messaging;
using RiotClient.Riot.Platform;

namespace RiotClient.Lobbies {
  public class GameLobby : Lobby {
    public EventHandler<PlayerCredentialsDto> GameStarted;
    public EventHandler<CustomLobby> GameCancelled;

    public EventHandler Updated;

    public GameDTO Data { get; protected set; }
    public List<GameMember> TeamOne { get; } = new List<GameMember>();
    public List<GameMember> TeamTwo { get; } = new List<GameMember>();
    public IEnumerable<GameMember> AllMembers => TeamOne.Concat(TeamTwo);

    public override GroupChat ChatLobby { get; protected set; }

    protected CustomLobby teamSelect;

    protected GameLobby() { }
    internal static GameLobby EnterChampSelect(GameDTO game) {
      var lobby = new GameLobby();
      Session.Current.CurrentLobby = lobby;
      RiotServices.GameService.SetClientReceivedGameMessage(game.Id, "CHAMP_SELECT_CLIENT");
      lobby.GotGameData(game);
      return lobby;
    }

    public virtual void LockIn() {
      RiotServices.GameService.ChampionSelectCompleted();
    }

    public virtual void SelectSpells(int one, int two) {
      RiotServices.GameService.SelectSpells(one, two);
    }

    public virtual void SelectSkin(int champ, int id) {
      RiotServices.GameService.SelectChampionSkin(champ, id);
    }

    public virtual void BanChampion(int champ) {
      RiotServices.GameService.BanChampion(champ);
    }

    public virtual void SelectChampion(int champ) {
      RiotServices.GameService.SelectChampion(champ);
    }

    public virtual async Task<ChampionBanInfoDTO[]> GetChampionsForBan() {
      return await RiotServices.GameService.GetChampionsForBan();
    }

    public override void Dispose() {
      ChatLobby.Dispose();
      RiotServices.GameService.QuitGame();
    }

    internal virtual void Associate(CustomLobby teamSelect) {
      this.teamSelect = teamSelect;
    }

    internal override bool HandleMessage(MessageReceivedEventArgs args) {
      GameDTO game = args.Body as GameDTO;
      PlayerCredentialsDto creds = args.Body as PlayerCredentialsDto;

      if (game != null) {
        GotGameData(game);
        return true;
      } else if (creds != null) {
        OnGameStart(creds);
        return true;
      }

      return false;
    }

    protected virtual void GotGameData(GameDTO game) {
      Data = game;

      if (ChatLobby == null) {
        ChatLobby = new GroupChat(new agsXMPP.Jid(game.RoomName.ToLower() + ".pvp.net"), game.RoomPassword);
      }

      var participants = game.TeamOne.Concat(game.TeamTwo);

      foreach (var thing in participants) {
        var player = thing as PlayerParticipant;
        var bot = thing as BotParticipant;
        int team;
        if (game.TeamOne.Contains(thing)) team = 0;
        else team = 1;

        GameMember now;

        if (player != null) {
          now = new GameMember(player, team, this);
        } else if (bot != null) {
          now = new GameMember(bot, team, this);
        } else throw new Exception("Unknown participant " + thing);

        GameMember old = AllMembers.SingleOrDefault(m => m.SummonerID == now.SummonerID);
        if (old != null) {
          old.Update(now);
        } else {
          if (team == 0) TeamOne.Add(now);
          else TeamTwo.Add(now);
          OnMemberJoined(now);
        }
      }

      if (game.GameState == "TEAM_SELECT") {
        Dispose();
        Session.Current.CurrentLobby = teamSelect;
        OnGameCancel(teamSelect);
      }

      OnUpdate(game);
      if (!loaded) OnLoaded();
    }

    protected virtual void OnGameStart(PlayerCredentialsDto creds) {
      teamSelect?.Dispose();
      GameStarted?.Invoke(this, creds);
    }

    protected virtual void OnGameCancel(CustomLobby lobby) {
      GameCancelled?.Invoke(this, lobby);
    }

    protected virtual void OnUpdate(GameDTO game) {
      Updated?.Invoke(this, new EventArgs());
    }
  }

  public class GameMember : LobbyMember {
    protected PlayerParticipant player;
    protected BotParticipant bot;

    public bool IsBot => bot != null;
    public bool IsPlayer => player != null;
    public int Team { get; protected set; }

    public override string Name {
      get {
        if (IsBot) return bot.SummonerName;
        else if (IsPlayer) return player.SummonerName;
        return null;
      }
    }
    public override long SummonerID {
      get {
        if (IsBot) return bot.Champion.ChampionId;
        else if (IsPlayer) return player.SummonerId;
        return -1;
      }
    }

    internal GameMember(PlayerParticipant player, int team, Lobby lobby) : base(lobby) {
      this.player = player;
      Team = team;
    }

    internal GameMember(BotParticipant bot, int team, Lobby lobby) : base(lobby) {
      this.bot = bot;
      Team = team;
    }

    internal virtual void Update(GameMember now) {
      Team = now.Team;

      if (now.IsPlayer && IsPlayer) player = now.player;
      else if (now.IsBot && IsBot) bot = now.bot;
      else throw new Exception("Cant update");

      OnChange();
    }
  }
}
