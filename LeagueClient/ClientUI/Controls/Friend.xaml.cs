using System;
using System.Collections.Generic;
using System.ComponentModel;
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
  public partial class Friend : UserControl, INotifyPropertyChanged {
    public event PropertyChangedEventHandler PropertyChanged;

    public BitmapImage SummonerIcon {
      get { return summonerIcon; }
      set { SetField(ref summonerIcon, value); }
    }
    public LeagueStatus Status {
      get { return status; }
      set { SetField(ref status, value); }
    }
    public string UserName {
      get { return name; }
      set { SetField(ref name, value); }
    }
    public string InGameString {
      get { return inGameString; }
      set { SetField(ref inGameString, value); }
    }
    public jabber.protocol.iq.Item User {
      get { return user; }
      set { SetField(ref user, value); }
    }
    public ChatConversation Conversation {
      get { return conversation; }
      set { SetField(ref conversation, value); }
    }
    public bool IsOffline {
      get { return isOffline; }
      set { SetField(ref isOffline, value); }
    }

    public GameDTO Game { get; private set; }
    public PublicSummoner Summoner { get; private set; }
    public RiotAPI.CurrentGameAPI.CurrentGameInfo GameInfo { get; private set; }

    private LeagueStatus status;
    private string name;
    private string inGameString;
    private jabber.protocol.iq.Item user;
    private ChatConversation conversation;
    private BitmapImage summonerIcon;
    private bool isOffline;

    public Friend(jabber.protocol.iq.Item item, ChatConversation convo) {
      Conversation = convo;
      User = item;
      UserName = item.Nickname;

      InitializeComponent();
    }

    public void Update(Presence p) {
      Status = new LeagueStatus(p.Status, p.Show);
      InGameString = Status.GameStatus.Value;
      if(Status.GameStatus == ChatStatus.inGame) {
        RiotServices.GameService.RetrieveInProgressSpectatorGameInfo(UserName).ContinueWith(FoundSpectatorInfo);
        RiotServices.SummonerService.GetSummonerByName(UserName).ContinueWith(GotSummoner);
      } else {
        Game = null;
        GameInfo = null;
      }
      Dispatcher.Invoke(() => {
        MsgText.Text = Status.Message;
        if (string.IsNullOrWhiteSpace(Status.Message)) {
          MsgText.Visibility = Visibility.Collapsed;
        } else {
          MsgText.Visibility = Visibility.Visible;
        }
        SummonerIcon = LeagueData.GetProfileIconImage(Status.ProfileIcon);
        switch (p.Show) {
          case "chat": InGameText.Foreground = App.ChatBrush; break;
          case "away": InGameText.Foreground = App.AwayBrush; break;
          case "dnd": InGameText.Foreground = App.BusyBrush; break;
        }
      });
    }

    public void Update() {
      if (Game == null) return;
      if (GameInfo == null) {
        long time = Status.TimeStamp - Client.GetMilliseconds();
        InGameString = $"In {QueueType.Values[Game.QueueTypeName]} for ~{TimeSpan.FromMilliseconds(time).ToString("m\\:ss")}";
      } else if (GameInfo.gameStartTime == 0) {
        InGameString = "Loading into " + QueueType.Values[Game.QueueTypeName];
      } else {
        long time = GameInfo.gameStartTime - Client.GetMilliseconds();
        InGameString = $"In {QueueType.Values[Game.QueueTypeName]} for {TimeSpan.FromMilliseconds(time).ToString("m\\:ss")}";
      }
    }

    private void SetField<T>(ref T field, T value, [System.Runtime.CompilerServices.CallerMemberName] string name = null) {
      if (!(field?.Equals(value) ?? false)) {
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
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
        App.Current.Dispatcher.Invoke(() => SummonerIcon = LeagueData.GetProfileIconImage(Status.ProfileIcon));
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SummonerIcon)));
      if (Status.GameStatus == ChatStatus.inGame)
        RiotAPI.CurrentGameAPI.BySummonerAsync("NA1", (long) Summoner.SummonerId)
          .ContinueWith(GotGameInfo);
    }

    private void GotGameInfo(Task<RiotAPI.CurrentGameAPI.CurrentGameInfo> game) {
      if (game.IsFaulted) {
        Client.Log("Failed to parse game [" + game.Exception.Message + "]");
        return;
      }
      if (game.Result == null) return;
      GameInfo = game.Result;
    }
  }
}
