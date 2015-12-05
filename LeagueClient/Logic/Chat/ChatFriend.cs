using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using jabber.protocol.client;
using jabber.protocol.iq;
using LeagueClient.ClientUI.Controls;
using LeagueClient.Logic.Riot;
using LeagueClient.Logic.Riot.Platform;
using MFroehlich.League.RiotAPI;

namespace LeagueClient.Logic.Chat {
  public class ChatFriend {
    public event EventHandler HistoryUpdated;
    public Dictionary<string, object> Data { get; } = new Dictionary<string, object>();

    public InvitationRequest Invite { get; set; }
    public Item User { get; }

    public PublicSummoner Summoner { get; private set; }
    public LeagueStatus Status { get; private set; }
    public bool IsOffline { get; private set; }
    public string Group { get; }

    public bool Unread { get; set; }
    public string History { get; private set; }

    public RiotAPI.CurrentGameAPI.CurrentGameInfo CurrentGameInfo { get; private set; }
    public GameDTO CurrentGameDTO { get; private set; }

    public ChatFriend(Item item) {
      User = item;

      var groups = item.GetGroups();
      Group = groups[0].InnerText;

      RiotServices.SummonerService.GetSummonerByName(User.Nickname).ContinueWith(GotSummoner);
    }

    public void ReceiveMessage(string message) {
      AppendMessage(User.Nickname, message);
    }

    public void SendMessage(string message) {
      AppendMessage(Client.LoginPacket.AllSummonerData.Summoner.Name, message);
      Client.ChatManager.SendMessage(User.JID, message);
    }

    public void UpdatePresence(Presence p) {
      if (!(IsOffline = (p?.Status == null))) {
        Status = new LeagueStatus(p.Status, p.Show);
        if (Status.GameStatus == ChatStatus.inGame) {
          RiotServices.GameService.RetrieveInProgressSpectatorGameInfo(User.Nickname).ContinueWith(GotGameDTO);
          if (Summoner != null) RiotAPI.CurrentGameAPI.BySummonerAsync("NA1", Summoner.SummonerId).ContinueWith(GotGameInfo);
        } else {
          CurrentGameDTO = null;
          CurrentGameInfo = null;
        }
      }
    }

    public double GetValue() {
      if (Status == null || IsOffline) return 1000;
      if (CurrentGameInfo != null) {
        if (CurrentGameInfo.gameStartTime == 0)
          return Status.GameStatus.Priority + .99;
        else
          return Status.GameStatus.Priority + 1.0 / CurrentGameInfo.gameStartTime;
      } else if (Status.Show == StatusShow.Away) {
        return Status.GameStatus.Priority + 100;
      } else
        return Status.GameStatus.Priority;
    }

    private void AppendMessage(string sender, string message) {
      History += $"[{sender}]: {message}\n";
      Unread = true;
      HistoryUpdated?.Invoke(this, new EventArgs());
    }

    #region Async Handlers
    private void GotSummoner(Task<PublicSummoner> task) {
      if (task.IsFaulted) {
        Client.Log(task.Exception);
        return;
      }
      if (task.Result == null) return;
      Summoner = task.Result;
      if (Status?.GameStatus == ChatStatus.inGame) {
        RiotAPI.CurrentGameAPI.BySummonerAsync("NA1", Summoner.SummonerId).ContinueWith(GotGameInfo);
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
          RiotAPI.CurrentGameAPI.BySummonerAsync("NA1", Summoner.SummonerId).ContinueWith(GotGameInfo);
        }) { IsBackground = true, Name = "GameFetch-" + Summoner.InternalName }.Start();
      } else Client.ChatManager.ForceUpdate();
    }
    #endregion
  }
}
