using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LeagueClient.ClientUI.Controls;
using LeagueClient.Logic.Riot;
using LeagueClient.Logic.Riot.Platform;
using MFroehlich.League.RiotAPI;
using agsXMPP.protocol.Base;
using agsXMPP.protocol.client;

namespace LeagueClient.Logic.Chat {
  public class ChatFriend {
    public event EventHandler HistoryUpdated;
    public Dictionary<string, object> Data { get; } = new Dictionary<string, object>();

    public InvitationRequest Invite { get; set; }
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
      AppendMessage(Client.LoginPacket.AllSummonerData.Summoner.Name, message);
      Client.ChatManager.SendMessage(User.Jid, message);
    }

    public void UpdatePresence(Presence p) {
      IsOffline = p.Status == null;
      if (!IsOffline) {

        Status = new LeagueStatus(p.Status, p.Show);
        if (Status.GameStatus == ChatStatus.inGame) {
          RiotServices.GameService.RetrieveInProgressSpectatorGameInfo(User.Name).ContinueWith(GotGameDTO);
          if (Cache != null) RiotAPI.CurrentGameAPI.BySummonerAsync("NA1", Cache.Data.Summoner.SummonerId).ContinueWith(GotGameInfo);
        } else {
          CurrentGameDTO = null;
          CurrentGameInfo = null;
        }

        Client.SummonerCache.GetData(User.Name, GotSummoner);
      }
    }

    public double GetValue() {
      if (Status == null || IsOffline) return 1000;
      double value = Status.GameStatus.Priority;
      var millis = Client.GetMilliseconds();
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
      if (Status?.GameStatus == ChatStatus.inGame) {
        RiotAPI.CurrentGameAPI.BySummonerAsync("NA1", Cache.Data.Summoner.SummonerId).ContinueWith(GotGameInfo);
      }
    }

    private void GotGameDTO(Task<PlatformGameLifecycleDTO> task) {
      if (task.IsFaulted) {
        Client.Log(task.Exception);
        return;
      }
      if (task.Result == null) return;
      CurrentGameDTO = task.Result.Game;
    }

    private void GotGameInfo(Task<RiotAPI.CurrentGameAPI.CurrentGameInfo> task) {
      if (task.IsFaulted) {
        Client.Log("Failed to parse game [" + task.Exception.Message + "]");
        return;
      }
      if (task.Result == null) return;
      CurrentGameInfo = task.Result;
      if (CurrentGameInfo.gameStartTime == 0) {
        new Thread(() => {
          Thread.Sleep(30000);
          RiotAPI.CurrentGameAPI.BySummonerAsync("NA1", Cache.Data.Summoner.SummonerId).ContinueWith(GotGameInfo);
        }) { IsBackground = true, Name = "GameFetch-" + Cache.Data.Summoner.InternalName }.Start();
      } else Client.ChatManager.ForceUpdate();
    }
    #endregion
  }
}
