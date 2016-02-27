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
  public class CustomGame : Game {
    public CustomLobby Lobby { get; private set; }

    private CustomGame() : base() { }

    public static async Task<CustomGame> Create(PracticeGameConfig config) {
      var lobby = new CustomGame();

      lobby.Lobby = CustomLobby.Create(lobby);
      GameDTO game = await RiotServices.GameService.CreatePracticeGame(config);

      if (game?.Name == null) {
        throw new Exception("Invalid name");
      } else {
        lobby.GotGameData(game);
        return lobby;
      }
    }

    internal static CustomGame Join(Invitation invite) {
      var lobby = new CustomGame();
      lobby.Lobby = CustomLobby.Join(invite, lobby);
      return lobby;
    }

    public override void CatchUp() {
      base.CatchUp();
      Lobby.CatchUp();
    }

    public virtual void SwitchTeams() {
      RiotServices.GameService.SwitchTeams(Data.Id);
    }

    public virtual void SwitchToObserver() {
      RiotServices.GameService.SwitchPlayerToObserver(Data.Id);
    }

    public virtual void SwitchToPlayer(int team) {
      RiotServices.GameService.SwitchObserverToPlayer(Data.Id, team);
    }

    public virtual async void StartChampSelect() {
      var start = await RiotServices.GameService.StartChampionSelection(Data.Id, Data.OptimisticLock);
      if (start.InvalidPlayers.Count != 0) {

      }
    }

    public virtual void Quit() {
      RiotServices.GameService.QuitGame();
      Lobby.Quit();
    }
  }
}
