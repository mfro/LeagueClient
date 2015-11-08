using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using jabber.protocol.client;
using jabber.protocol.iq;
using LeagueClient.ClientUI.Controls;
using LeagueClient.Logic.Riot;
using LeagueClient.Logic.Riot.Platform;
using MFroehlich.League.RiotAPI;

namespace LeagueClient.Logic.Chat {
  public class ChatFriend {
    public Item User { get; }
    public FriendListItem ListItem { get; }
    public ChatConversation Conversation { get; }

    public PublicSummoner Summoner { get; private set; }
    public LeagueStatus Status { get; private set; }
    public bool IsOffline { get; private set; }

    public RiotAPI.CurrentGameAPI.CurrentGameInfo CurrentGameInfo { get; private set; }
    public GameDTO CurrentGameDTO { get; private set; }

    public ChatFriend(Item item) {
      Conversation = new ChatConversation(item.Nickname, item.JID.User);
      User = item;
      ListItem = new FriendListItem(this);

      RiotServices.SummonerService.GetSummonerByName(User.Nickname).ContinueWith(GotSummoner);
    }

    public void UpdatePresence(Presence p) {
      if (!(IsOffline = (p?.Status == null))) {
        Status = new LeagueStatus(p.Status, p.Show);
        if (Status.GameStatus == ChatStatus.inGame) {
          RiotServices.GameService.RetrieveInProgressSpectatorGameInfo(User.Nickname).ContinueWith(GotGameDTO);
          if(Summoner != null) RiotAPI.CurrentGameAPI.BySummonerAsync("NA1", Summoner.SummonerId).ContinueWith(GotGameInfo);
        }
      }
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
    }
    #endregion
  }
}
