using RiotClient.Riot.Platform;
using RtmpSharp.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RiotClient.Lobbies {
  public class Queue {
    public event EventHandler<GameDTO> QueuePopped;
    public event EventHandler<QueuePopPlayerState[]> QueuePopUpdated;
    public event EventHandler<GameLobby> EnteredChampSelect;
    public event EventHandler QueueCancelled;

    public bool HasPopped { get; private set; }

    public DateTime Start { get; }
    public TimeSpan Elapsed => DateTime.Now - Start;

    private Queue(QueueInfo info) {
      Start = DateTime.Now;
      Session.Current.CurrentQueue = this;
    }

    public bool HandleMessage(MessageReceivedEventArgs args) {
      var game = args.Body as GameDTO;

      if (game != null) {
        switch (game.GameState) {
          case "JOINING_CHAMP_SELECT": //QUEUE POP
            if (!HasPopped) {
              OnQueuePop(game);
            }

            OnQueuePopUpdated(game);
            break;

          case "CHAMP_SELECT":
            OnEnteredChampSelect(game);
            break;

          case "TERMINATED":
            OnQueueCancelled();
            break;
        }

        return true;
      }

      return false;
    }

    public void React(bool accept) {
      RiotServices.GameService.AcceptPoppedGame(accept);
    }

    public void Cancel() {
      RiotServices.MatchmakerService.CancelFromQueueIfPossible(Session.Current.Account.SummonerID);
      RiotServices.MatchmakerService.PurgeFromQueues();
      OnQueueCancelled();
    }

    public static async Task<Queue> Create(MatchMakerParams mmp) {
      return Create(await RiotServices.MatchmakerService.AttachToQueue(mmp));
    }

    public static Queue Create(SearchingForMatchNotification searching) {
      if (searching.PlayerJoinFailures != null) {
        var leaver = searching.PlayerJoinFailures[0];
        bool me = leaver.Summoner.SummonerId == Session.Current.Account.SummonerID;
        switch (leaver.ReasonFailed) {
          case "LEAVER_BUSTER":
            throw new Exception("Leaver Buster");
          //TODO Leaverbuster
          case "QUEUE_DODGER":
            throw new Exception("Queue Dodger");
          //TODO Queue delay event
          default:
            throw new Exception(leaver.ReasonFailed);
        }
      } else if (searching.JoinedQueues != null && searching.JoinedQueues.Count > 0) {
        if (searching.JoinedQueues.Count != 1)
          Session.Log("Received incorrect number of joined queues");
        return new Queue(searching.JoinedQueues.FirstOrDefault());
      } else throw new Exception("Unknown exception");
    }

    protected void OnQueuePop(GameDTO game) {
      HasPopped = true;
      QueuePopped?.Invoke(this, game);
    }

    protected void OnQueuePopUpdated(GameDTO game) {
      var states = new QueuePopPlayerState[game.StatusOfParticipants.Length];
      for (int i = 0; i < game.StatusOfParticipants.Length; i++) {
        switch (game.StatusOfParticipants[i]) {
          case '0':
            states[i] = QueuePopPlayerState.None;
            break;
          case '1':
            states[i] = QueuePopPlayerState.Accepted;
            break;
          case '2':
            states[i] = QueuePopPlayerState.Declined;
            break;
        }
      }
    }

    protected void OnEnteredChampSelect(GameDTO game) {
      EnteredChampSelect?.Invoke(this, GameLobby.EnterChampSelect(game));
    }

    protected void OnQueueCancelled() {
      QueueCancelled?.Invoke(this, new EventArgs());
    }

    public enum QueuePopPlayerState {
      None, Accepted, Declined
    }
  }
}
