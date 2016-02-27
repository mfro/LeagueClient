using RiotClient.Riot.Platform;
using RtmpSharp.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RiotClient.Lobbies {
  public class Game {
    public event EventHandler<PlayerCredentialsDto> GameStarted;

    public event EventHandler<GameMemberEventArgs> MemberChangedTeam;
    public event EventHandler<GameMemberEventArgs> MemberJoined;
    public event EventHandler<GameMemberEventArgs> MemberLeft;

    public event EventHandler EnteredChampSelect;

    public event EventHandler Updated;

    public GameDTO Data { get; private set; }
    public List<GameMember> TeamOne { get; } = new List<GameMember>();
    public List<GameMember> TeamTwo { get; } = new List<GameMember>();
    public List<GameMember> Observers { get; } = new List<GameMember>();
    public IEnumerable<GameMember> AllMembers => TeamOne.Concat(TeamTwo).Concat(Observers);

    protected Game() {
      Session.Current.CurrentGame = this;
    }

    internal Game(GameDTO game) : this() {
      Data = game;
      RiotServices.GameService.SetClientReceivedGameMessage(game.Id, "CHAMP_SELECT_CLIENT");
      OnUpdate(game);
    }

    internal virtual bool HandleMessage(MessageReceivedEventArgs args) {
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

    public virtual void CatchUp() {
      foreach (var member in AllMembers) {
        OnMemberJoined(member);
      }
      if (Data != null) OnUpdate(Data);
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

    protected virtual void GotGameData(GameDTO game) {
      Data = game;

      var participants = game.TeamOne.Concat(game.TeamTwo);
      var left = new List<GameMember>(AllMembers);
      foreach (var thing in participants) {
        var player = thing as PlayerParticipant;
        var bot = thing as BotParticipant;
        int team;
        if (game.TeamOne.Contains(thing)) team = 0;
        else team = 1;

        GameMember now;

        if (player != null) {
          now = new GameMember(player, team);
        } else if (bot != null) {
          now = new GameMember(bot, team);
        } else throw new Exception("Unknown participant " + thing);

        GameMember old = AllMembers.SingleOrDefault(m => m.SummonerID == player.SummonerId);
        if (old != null) {
          RemoveMember(old);

          bool diffTeam = old.Team != now.Team;
          old.Update(now);
          AddMember(old);
          if (diffTeam) OnMemberChangeTeam(old);
          left.Remove(old);
        } else {
          AddMember(now);
          OnMemberJoined(now);
        }
      }

      foreach (var thing in game.Observers) {
        GameMember now = new GameMember(thing);

        GameMember old = AllMembers.SingleOrDefault(m => m.SummonerID == thing.SummonerId);

        if (old != null) {
          RemoveMember(old);

          bool diffTeam = old.Team != now.Team;
          old.Update(now);
          AddMember(old);
          if (diffTeam) OnMemberChangeTeam(old);
          left.Remove(old);
        } else {
          AddMember(now);
          OnMemberJoined(now);
        }
      }

      foreach (var id in left) {
        OnMemberLeft(id);
        RemoveMember(id);
      }

      if (game.GameState == "CHAMP_SELECT" || game.GameState == "PRE_CHAMP_SELECT") {
        OnEnteredChampSelect();
      }
      OnUpdate(game);
    }

    protected virtual void OnGameStart(PlayerCredentialsDto creds) {
      GameStarted?.Invoke(this, creds);
    }

    protected virtual void OnMemberChangeTeam(GameMember member) {
      MemberChangedTeam?.Invoke(this, new GameMemberEventArgs(member));
    }

    protected virtual void OnMemberJoined(GameMember member) {
      MemberJoined?.Invoke(this, new GameMemberEventArgs(member));
    }

    protected virtual void OnMemberLeft(GameMember member) {
      MemberLeft?.Invoke(this, new GameMemberEventArgs(member));
    }

    protected virtual void OnUpdate(GameDTO game) {
      Updated?.Invoke(this, new EventArgs());
    }

    protected virtual void OnEnteredChampSelect() {
      RiotServices.GameService.SetClientReceivedGameMessage(Data.Id, "CHAMP_SELECT_CLIENT");
      EnteredChampSelect?.Invoke(this, new EventArgs());
    }

    private void AddMember(GameMember member) {
      if (member.Team == 0) TeamOne.Add(member);
      else if (member.Team == 1) TeamTwo.Add(member);
      else if (member.Team == 2) Observers.Add(member);
      else throw new Exception("Unknown team " + member.Team);
    }

    private void RemoveMember(GameMember member) {
      if (member.Team == 0) TeamOne.Remove(member);
      else if (member.Team == 1) TeamTwo.Remove(member);
      else if (member.Team == 2) Observers.Remove(member);
      else throw new Exception("Unknown team " + member.Team);
    }
  }

  public class GameMember {
    public event EventHandler Changed;

    private PlayerParticipant player;
    private GameObserver observer;
    private BotParticipant bot;

    public bool IsBot => bot != null;
    public bool IsPlayer => player != null;
    public bool IsObserver => observer != null;
    public int Team { get; private set; }

    public long SummonerID {
      get {
        if (IsObserver) return observer.SummonerId;
        else if (IsPlayer) return player.SummonerId;
        else if (IsBot) return bot.Champion.ChampionId;
        return -1;
      }
    }
    public string Name {
      get {
        if (IsObserver) return observer.SummonerName;
        else if (IsPlayer) return player.SummonerName;
        else if (IsBot) return bot.SummonerName;
        return null;
      }
    }
    public bool IsMe => SummonerID == Session.Current.Account.SummonerID;

    public GameMember(PlayerParticipant player, int team) {
      this.player = player;
      Team = team;
    }

    public GameMember(BotParticipant bot, int team) {
      this.bot = bot;
      Team = team;
    }

    public GameMember(GameObserver observer) {
      this.observer = observer;
      Team = 2;
    }

    internal void Update(GameMember now) {
      Team = now.Team;
      if (now.IsPlayer && IsPlayer) player = now.player;
      else if (now.IsBot && IsBot) bot = now.bot;
      else if (now.IsObserver && IsObserver) observer = now.observer;
      else throw new Exception("Cant update");
      Changed?.Invoke(this, new EventArgs());
    }
  }

  public class GameMemberEventArgs {
    public GameMember Member { get; }

    public GameMemberEventArgs(GameMember member) {
      Member = member;
    }
  }
}
