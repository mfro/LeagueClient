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
    public event EventHandler Updated;

    public GameDTO Data { get; private set; }

    public Game(GameDTO game) {
      Data = game;
      Session.Current.CurrentGame = this;
      RiotServices.GameService.SetClientReceivedGameMessage(game.Id, "CHAMP_SELECT_CLIENT");
      OnUpdate(game);
    }

    internal virtual bool HandleMessage(MessageReceivedEventArgs args) {
      GameDTO game = args.Body as GameDTO;
      PlayerCredentialsDto creds = args.Body as PlayerCredentialsDto;

      if (game != null) {
        OnUpdate(game);
        return true;
      } else if (creds != null) {
        OnGameStart(creds);
        return true;
      }

      return false;
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

    private void OnGameStart(PlayerCredentialsDto creds) {
      GameStarted?.Invoke(this, creds);
    }

    private void OnUpdate(GameDTO game) {
      Data = game;
      Updated?.Invoke(this, new EventArgs());
    }
  }
}
