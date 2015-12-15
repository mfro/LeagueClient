using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using LeagueClient.ClientUI.Controls;
using LeagueClient.ClientUI.Main;
using LeagueClient.Logic;
using LeagueClient.Logic.Chat;
using LeagueClient.Logic.Queueing;
using LeagueClient.Logic.Riot.Platform;
using MFroehlich.League.Assets;
using RtmpSharp.Messaging;
using LeagueClient.Logic.Riot;
using MFroehlich.Parsing.JSON;
using LeagueClient.Logic.Riot.Team;
using agsXMPP.protocol.client;

namespace LeagueClient.ClientUI {
  /// <summary>
  /// Interaction logic for LandingPage.xaml
  /// </summary>
  public partial class LandingPage : Page, IClientPage, IQueueManager {
    public IQueueInfo CurrentInfo { get; private set; }
    public IQueuePopup CurrentPopup { get; private set; }
    public IClientSubPage CurrentPage { get; private set; }
    public BindingList<ChatFriend> OpenChatsList { get; } = new BindingList<ChatFriend>();

    private Border currentButton;

    private List<Border> Buttons;
    private int[] ArrowLocations = new[] { 11, 53, 95, 134, 172 };

    public LandingPage() {
      InitializeComponent();
      Buttons = new List<Border> { LogoutTab, PlayTab, FriendsTab, ProfileTab, ShopTab };

      ShowTab(Tab.Friends);
      IPAmount.Content = Client.LoginPacket.IpBalance.ToString();
      RPAmount.Content = Client.LoginPacket.RpBalance.ToString();
      NameLabel.Content = Client.LoginPacket.AllSummonerData.Summoner.Name;
      ProfileIcon.Source = LeagueData.GetProfileIconImage(LeagueData.GetIconData(Client.LoginPacket.AllSummonerData.Summoner.ProfileIconId));

      Client.ChatManager.FriendList.ListChanged += FriendList_ListChanged;
      Client.ChatManager.StatusUpdated += ChatManager_StatusUpdated;
      Client.ChatManager.MessageReceived += ChatManager_MessageReceived;

      OpenChats.ItemsSource = OpenChatsList;
      Popup.IconSelector.IconSelected += IconSelector_IconSelected;
    }

    private void ChatManager_MessageReceived(object sender, Message e) {
      if (!Client.ChatManager.Friends.ContainsKey(e.From.User)) return;
      var friend = Client.ChatManager.Friends[e.From.User];
      if (!OpenChatsList.Contains(friend)) Dispatcher.Invoke(() => OpenChatsList.Add(friend));
    }

    private void FriendList_ListChanged(object sender, ListChangedEventArgs e) {
      //var groups = new Dictionary<object, List<ChatFriend>> {
      //  [ShowType.chat] = new List<ChatFriend>(),
      //  [ShowType.away] = new List<ChatFriend>(),
      //  [ShowType.dnd] = new List<ChatFriend>()
      //};
      //foreach (var item in Client.ChatManager.FriendList) {
      //  if (item.CurrentGameInfo != null) {
      //    if (!groups.ContainsKey(item.CurrentGameInfo.gameId))
      //      groups[item.CurrentGameInfo.gameId] = new List<ChatFriend>();
      //    groups[item.CurrentGameInfo.gameId].Add(item);
      //  }
      //}
      //foreach (var item in new List<object>(groups.Keys)) {
      //  if (groups[item].Count == 1) groups.Remove(item);
      //}

      //foreach (var item in Client.ChatManager.FriendList) {
      //  if (groups.Any(pair => pair.Value.Contains(item))) continue;
      //  groups[item.Status.Show].Add(item);
      //}
      Dispatcher.Invoke(() => {
        GroupList.ItemsSource = Client.ChatManager.FriendList;
        //GroupList.Children.Clear();
        //foreach (var group in groups.Where(pair => pair.Value.Count > 0).OrderBy(pair => pair.Value.First().GetValue())) {
        //  GroupList.Children.Add(new ItemsControl { ItemsSource = group.Value.OrderBy(u => u.GetValue()) });
        //}
      });
    }

    private void ChatManager_StatusUpdated(object sender, StatusUpdatedEventArgs e) {
      Dispatcher.Invoke(() => {
        switch (e.PresenceType) {
          case PresenceType.available:
            if (e.Status.GameStatus == ChatStatus.outOfGame) {
              if (!string.IsNullOrWhiteSpace(e.Status.Message))
                CurrentStatus.Content = e.Status.Message;
              else if (e.Show == ShowType.chat)
                CurrentStatus.Content = "Online";
              else CurrentStatus.Content = "Away";
            } else CurrentStatus.Content = e.Status.GameStatus.Value;

            switch (e.Show) {
              case ShowType.away: CurrentStatus.Foreground = App.AwayBrush; break;
              case ShowType.chat: CurrentStatus.Foreground = App.ChatBrush; break;
              case ShowType.dnd: CurrentStatus.Foreground = App.BusyBrush; break;
            }

            break;
          case PresenceType.invisible:
            CurrentStatus.Content = "Invisible";
            CurrentStatus.Foreground = App.ForeBrush;
            break;
        }
      });
    }

    #region Tab Events
    private void Tab_MouseEnter(object sender, RoutedEventArgs e) {
      var text = ((Border) sender).Child as TextBlock;
      text.Foreground = Brushes.White;
    }

    private void Tab_MouseLeave(object sender, RoutedEventArgs e) {
      if (sender != currentButton) {
        var text = ((Border) sender).Child as TextBlock;
        text.Foreground = App.FontBrush;
      }
    }

    private void Tab_MousePress(object sender, RoutedEventArgs e) {
      ShowTab((Tab) Buttons.IndexOf((Border) sender));
    }

    private void ShowTab(Tab tab) {
      if (currentButton != null) ((TextBlock) currentButton.Child).Foreground = App.FontBrush;
      currentButton = Buttons[(int) tab];

      int pageHeight = (int) (double) FindResource("PageHeight");
      SlidingGrid.Height = pageHeight * (Buttons.Count - 1);

      if (tab == Tab.Logout) Client.Logout();

      var arrowAnim = new ThicknessAnimation(new Thickness(15, ArrowLocations[(int) tab], 15, 0), new Duration(TimeSpan.FromMilliseconds(100)));
      Arrows.BeginAnimation(MarginProperty, arrowAnim);

      var slideAnim = new ThicknessAnimation(new Thickness(0, -pageHeight * ((int) tab - 1), 0, 0), new Duration(TimeSpan.FromMilliseconds(100)));
      SlidingGrid.BeginAnimation(MarginProperty, slideAnim);

      if (tab == Tab.Play) PlayPage.Reset();
      if (tab == Tab.Shop) {
        RiotServices.LoginService.GetStoreUrl().ContinueWith(t => {
          Client.Log(t.Result);
          ShopBrowser.Source = new Uri(t.Result);
        });
      }
    }
    #endregion

    #region Other Events
    private void Info_Popped(object sender, EventArgs e) {
      Client.QueueManager.ShowInfo(null);
    }

    private void CurrentPopup_Close(object sender, QueueEventArgs e) {
      Dispatcher.Invoke(() => PopupPanel.BeginStoryboard(App.FadeOut));
      CurrentPopup.Close -= CurrentPopup_Close;
      CurrentPopup = null;
    }

    private void Grid_MouseDown(object sender, MouseButtonEventArgs e) {
      //if (e.GetPosition((Grid) sender).Y < 20) {
      if (e.ClickCount == 2) Client.MainWindow.Center();
      else Client.MainWindow.DragMove();
      //}
    }

    private void Friend_MouseUp(object sender, MouseButtonEventArgs e) {
      if (e.ChangedButton == MouseButton.Left && !OpenChatsList.Contains(((FriendListItem2) sender).friend))
        OpenChatsList.Add(((FriendListItem2) sender).friend);
    }

    private void CurrentStatus_MouseUp(object sender, MouseButtonEventArgs e) {
      switch (Client.ChatManager.Show) {
        case ShowType.away:
          Client.ChatManager.Show = ShowType.chat;
          break;
        case ShowType.chat:
          Client.ChatManager.Show = ShowType.away;
          break;
      }
    }

    private void ProfileIcon_Click(object sender, MouseButtonEventArgs e) {
      Popup.CurrentSelector = PopupSelector.Selector.ProfileIcons;
      Popup.BeginStoryboard(App.FadeIn);
    }

    private void IconSelector_IconSelected(object sender, Icon e) {
      Popup.BeginStoryboard(App.FadeOut);
      ProfileIcon.Source = LeagueData.GetProfileIconImage(LeagueData.GetIconData(e.IconId));
    }

    private void Popup_Close(object sender, EventArgs e) {
      Popup.BeginStoryboard(App.FadeOut);
    }

    private void ChatConversation_ChatClosed(object sender, EventArgs e) {
      OpenChatsList.Remove(((ChatConversation) sender).friend);
    }

    private void Animate(Button butt, string key) => butt.BeginStoryboard((Storyboard) butt.FindResource(key));
    #endregion

    #region Interface
    bool IClientPage.HandleMessage(MessageReceivedEventArgs args) {
      if (CurrentPopup?.HandleMessage(args) ?? false) return true;
      if (CurrentInfo?.HandleMessage(args) ?? false) return true;
      if (CurrentPage != null) return CurrentPage.HandleMessage(args);
      return PlayPage.HandleMessage(args);
    }

    void IQueueManager.ShowQueuePopup(IQueuePopup popup) {
      CurrentPopup = popup;
      CurrentPopup.Close += CurrentPopup_Close;

      PopupPanel.BeginStoryboard(App.FadeIn);
      PopupPanel.Child = popup.Control;
    }

    void IQueueManager.ShowPage() {
      if (Thread.CurrentThread != Dispatcher.Thread) { Dispatcher.Invoke(Client.QueueManager.ShowPage); return; }

      ShowTab(Tab.Play);
    }

    void IQueueManager.ShowPage(IClientSubPage page) {
      if (Thread.CurrentThread != Dispatcher.Thread) { Dispatcher.MyInvoke(Client.QueueManager.ShowPage, page); return; }

      CloseSubPage(true);
      page.Close += HandlePageClose;
      CurrentPage = page;
      SubPageArea.Content = page?.Page;
      SubPageArea.Visibility = Visibility.Visible;
      ShowTab(Tab.Play);
    }

    void IQueueManager.ShowNotification(Alert alert) {
      //throw new NotImplementedException();
    }

    void IQueueManager.ShowInfo(IQueueInfo info) {
      CurrentInfo = info;
      if (info != null) {
        info.Popped += Info_Popped;
        Dispatcher.Invoke(() => QueuerArea.Child = info.Control);
      }
    }

    void IQueueManager.BeginChampionSelect(GameDTO game) {
      var page = new ChampSelectPage(game);
      Client.QueueManager.ShowPage(page);
      Client.ChatManager.Status = ChatStatus.championSelect;
      RiotServices.GameService.SetClientReceivedGameMessage(game.Id, "CHAMP_SELECT_CLIENT");
    }

    bool IQueueManager.AttachToQueue(SearchingForMatchNotification result) {
      if (result.PlayerJoinFailures != null) {
        var leaver = result.PlayerJoinFailures[0];
        bool me = leaver.Summoner.SumId == Client.LoginPacket.AllSummonerData.Summoner.SumId;
        switch (leaver.ReasonFailed) {
          case "LEAVER_BUSTER":
            //TODO Leaverbuster
            break;
          case "QUEUE_DODGER":
            Dispatcher.Invoke(() => Client.QueueManager.ShowInfo(new BingeQueuer(leaver.PenaltyRemainingTime, me ? null : leaver.Summoner.Name)));
            break;
        }
        return false;
      } else if (result.JoinedQueues != null) {
        return true;
      } else return false;
    }

    void IQueueManager.AcceptInvite(InvitationRequest invite) {
      var task = RiotServices.GameInvitationService.Accept(invite.InvitationId);
      var metaData = JSONParser.ParseObject(invite.GameMetaData, 0);
      if ((int) metaData["gameTypeConfigId"] == 12) {
        var lobby = new CapLobbyPage(false);
        task.ContinueWith(t => lobby.GotLobbyStatus(task.Result));
        RiotServices.CapService.JoinGroupAsInvitee((string) metaData["groupFinderId"]);
        Client.QueueManager.ShowPage(lobby);
      } else {
        switch ((string) metaData["gameType"]) {
          case "PRACTICE_GAME":
            var custom = new CustomLobbyPage();
            task.ContinueWith(t => custom.GotLobbyStatus(task.Result));
            Client.QueueManager.ShowPage(custom);
            break;
          case "NORMAL_GAME":
            var normal = new DefaultLobbyPage(new MatchMakerParams { QueueIds = new[] { (int) metaData["queueId"] } });
            task.ContinueWith(t => normal.GotLobbyStatus(task.Result));
            Client.QueueManager.ShowPage(normal);
            break;
          case "RANKED_TEAM_GAME":
            var ranked = new DefaultLobbyPage(new MatchMakerParams { QueueIds = new[] { (int) metaData["queueId"] }, TeamId = new TeamId { FullId = (string) metaData["rankedTeamId"] } });
            task.ContinueWith(t => ranked.GotLobbyStatus(task.Result));
            Client.QueueManager.ShowPage(ranked);
            break;
        }
      }
    }

    void IQueueManager.ViewProfile(string summonerName) {
      Client.SummonerCache.GetData(summonerName, Profile.GotSummoner);
      Dispatcher.MyInvoke(ShowTab, Tab.Profile);
    }
    #endregion

    private void CloseSubPage(bool notifyPage) {
      if (Thread.CurrentThread != Dispatcher.Thread) { Dispatcher.MyInvoke(CloseSubPage, notifyPage); return; }

      if (CurrentPage != null) {
        CurrentPage.Close -= HandlePageClose;
        SubPageArea.Content = null;
        CurrentPage = null;
        SubPageArea.Visibility = Visibility.Collapsed;
      }
    }

    private void HandlePageClose(object source, EventArgs e) {
      if (Thread.CurrentThread != Dispatcher.Thread)
        Dispatcher.MyInvoke(CloseSubPage, false);
      else
        CloseSubPage(false);
    }

    private enum Tab {
      Logout = 0,
      Play = 1,
      Friends = 2,
      Profile = 3,
      Shop = 4
    }
  }
}
