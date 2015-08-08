using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using jabber.protocol.client;
using LeagueClient.Logic;
using LeagueClient.Logic.Chat;
using LeagueClient.Logic.Riot;
using LeagueClient.Logic.Riot.Platform;
using MFroehlich.League.Assets;
using MFroehlich.League.RiotAPI;

namespace LeagueClient.ClientUI.Controls {
  /// <summary>
  /// Interaction logic for Friend.xaml
  /// </summary>
  public partial class Friend : UserControl {
    public string UserName { get; private set; }
    public string InGame { get; private set; }
    public LeagueStatus Status { get; private set; }
    public jabber.protocol.iq.Item User { get; private set; }
    public ChatConversation Conversation { get; private set; }

    public GameDTO Game { get; private set; }
    public RiotAPI.CurrentGameAPI.CurrentGameInfo GameInfo { get; private set; }
    public PublicSummoner Summoner { get; private set; }

    public Friend(Presence p, jabber.protocol.iq.Item item, ChatConversation convo) {
      Status = new LeagueStatus(p.Status, p.Show);
      Conversation = convo;
      User = item;
      UserName = User.Nickname;
      InGame = Status.GameStatus.Name;
      InitializeComponent();
      ProfileIcon.Source = LeagueData.GetProfileIconImage(Status.ProfileIcon);
      NameText.Text = UserName;
      if (Status.Message.Length > 0) MsgText.Text = Status.Message;
      else MsgText.Visibility = System.Windows.Visibility.Collapsed;
      InGameText.Text = InGame;
      RiotCalls.GameService.RetrieveInProgressSpectatorGameInfo(UserName).ContinueWith(FoundSpectatorInfo);
      RiotCalls.SummonerService.GetSummonerByName(UserName).ContinueWith(GotSummoner);

      switch (p.Show) {
        case "chat": InGameText.Foreground = App.ChatBrush; break;
        case "away": InGameText.Foreground = App.AwayBrush; break;
        case "dnd": InGameText.Foreground = App.BusyBrush; break;
      }
    }

    private void FoundSpectatorInfo(Task<PlatformGameLifecycleDTO> t) {
      if (t.IsFaulted) {
        Client.Log(t.Exception);
        return;
      }
      if (t.Result == null) return;
      Game = t.Result.Game;
    }

    private void GotSummoner(Task<PublicSummoner> summ) {
      if (summ.IsFaulted) {
        Client.Log(summ.Exception);
        return;
      }
      if (summ.Result == null) return;
      Summoner = summ.Result;
      if (Summoner.ProfileIconId != Status.ProfileIcon)
        Dispatcher.Invoke(new Action(() =>
          ProfileIcon.Source = LeagueData.GetProfileIconImage(Summoner.ProfileIconId)));
      if(Status.GameStatus == LeagueStatus.InGame)
        RiotAPI.CurrentGameAPI.BySummonerAsync("NA1", (long) Summoner.SummonerId)
          .ContinueWith(GotGameInfo);
    }

    private void GotGameInfo(Task<RiotAPI.CurrentGameAPI.CurrentGameInfo> game) {
      if (game.IsFaulted) {
        Client.Log(game.Exception);
        return;
      }
      if (game.Result == null) return;
      GameInfo = game.Result;
      Dispatcher.Invoke(Update);
    } 

    public void Update() {
      if (Game == null) return;
      if (GameInfo == null) {
        long time = Status.TimeStamp - Client.GetMilliseconds();
        InGameText.Text = $"In {QueueType.Values[Game.QueueTypeName]} for ~{TimeSpan.FromMilliseconds(time).ToString("m\\:ss")}";
      } else if (GameInfo.gameStartTime == 0) {
        InGameText.Text = "Loading into " + QueueType.Values[Game.QueueTypeName];
      } else {
        long time = GameInfo.gameStartTime - Client.GetMilliseconds();
        InGameText.Text = $"In {QueueType.Values[Game.QueueTypeName]} for {TimeSpan.FromMilliseconds(time).ToString("m\\:ss")}";
      }
    }
  }
}
