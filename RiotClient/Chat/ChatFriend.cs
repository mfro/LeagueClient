using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using agsXMPP.protocol.Base;
using agsXMPP.protocol.client;
using RiotClient.Riot.Platform;
using MFroehlich.League.RiotAPI;
using RiotClient.Lobbies;

namespace RiotClient.Chat {
  public class ChatFriend {
    public event EventHandler HistoryUpdated;
    //public Dictionary<string, object> Data { get; } = new Dictionary<string, object>();

    public Invitation Invite { get; set; }
    public RosterItem User { get; }

    public SummonerCache.Item Cache { get; private set; }
    public LeagueStatus Status { get; private set; }
    public bool IsOffline { get; private set; }
    public string Group { get; }

    public bool Unread { get; set; }
    public string History { get; private set; }

    public RiotAPI.CurrentGameAPI.CurrentGameInfo CurrentGameInfo { get; private set; }
    public GameDTO CurrentGameDTO { get; private set; }

    public ChatFriend(RosterItem item) {
      User = item;

      var groups = item.GetGroups();
      Group = groups.Item(0).InnerXml;
    }

    public void ReceiveMessage(string message) {
      AppendMessage(User.Name, message);
    }

    public void SendMessage(string message) {
      AppendMessage(Session.Current.Account.Name, message);
      Session.Current.ChatManager.SendMessage(User.Jid, message);
    }

    public void UpdatePresence(Presence p) {
      IsOffline = p.Status == null;
      if (!IsOffline) {
        Status = new LeagueStatus(p.Status, p.Show);
        if (Status.GameStatus == ChatStatus.inGame) {
          RiotServices.GameService.RetrieveInProgressSpectatorGameInfo(User.Name).ContinueWith(GotGameDTO);
          if (Cache != null)
            CurrentGameFetcher.FetchGame(Cache.Data.Summoner.SummonerId, GotGameInfo);
        } else {
          CurrentGameDTO = null;
          CurrentGameInfo = null;
        }

        Session.Current.SummonerCache.GetData(User.Name, GotSummoner);
        Session.Current.ChatManager.ForceUpdate();
      }
    }

    public double GetValue() {
      if (Status == null || IsOffline) return 1000;
      double value = Status.GameStatus.Priority;
      var millis = Session.GetMilliseconds();
      if (CurrentGameInfo != null && CurrentGameInfo.gameStartTime != 0) {
        value += (1.0 / (millis - CurrentGameInfo.gameStartTime));
      } else if (Status.GameStatus == ChatStatus.inGame && Status.TimeStamp != 0) {
        value += (1.0 / (millis - Status.TimeStamp));
      } else if (Status.Show == ShowType.away) {
        value += 100;
      }
      return value;
    }

    private void AppendMessage(string sender, string message) {
      History += $"[{sender}]: {message}\n";
      Unread = true;
      HistoryUpdated?.Invoke(this, new EventArgs());
    }

    #region Async Handlers

    private void GotSummoner(SummonerCache.Item item) {
      Cache = item;
      if (Status?.GameStatus == ChatStatus.inGame)
        CurrentGameFetcher.FetchGame(Cache.Data.Summoner.SummonerId, GotGameInfo);
    }

    private void GotGameDTO(Task<PlatformGameLifecycleDTO> task) {
      if (task.IsFaulted) {
        Session.Log(task.Exception);
        return;
      }
      if (task.Result == null) return;
      CurrentGameDTO = task.Result.Game;
    }

    private void GotGameInfo(RiotAPI.CurrentGameAPI.CurrentGameInfo game) {
      CurrentGameInfo = game;
      Session.Current.ChatManager.ForceUpdate();
    }

    #endregion
  }
}
