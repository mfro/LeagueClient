using agsXMPP.protocol.client;
using LeagueClient.Logic;
using LeagueClient.Logic.Chat;
using LeagueClient.Logic.Queueing;
using LeagueClient.UI.Client.Alerts;
using LeagueClient.UI.Client.Custom;
using LeagueClient.UI.Client.Friends;
using LeagueClient.UI.Client.Lobbies;
using LeagueClient.UI.Selectors;
using MFroehlich.League.Assets;
using MFroehlich.League.RiotAPI;
using MFroehlich.Parsing.JSON;
using RiotClient;
using RiotClient.Chat;
using RiotClient.Lobbies;
using RiotClient.Riot.Platform;
using RtmpSharp.Messaging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
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
using System.Xml.Linq;

namespace LeagueClient.UI.Client {
  /// <summary>
  /// Interaction logic for LandingPage.xaml
  /// </summary>
  public sealed partial class LandingPage : Page, IQueueManager {
    public IQueueInfo CurrentInfo { get; private set; }
    public IQueuePopup CurrentPopup { get; private set; }
    public IClientSubPage CurrentPage { get; private set; }
    public BindingList<ChatFriend> OpenChatsList { get; } = new BindingList<ChatFriend>();

    public LandingPage() {
      InitializeComponent();

      Session.Current.ChatManager.StatusUpdated += ChatManager_StatusUpdated;
      Session.Current.ChatManager.MessageReceived += ChatManager_MessageReceived;

      ChatCombo.ItemsSource = Enum.GetValues(typeof(ChatMode));
      ChatCombo.SelectedItem = ChatMode.Chat;

      ShowTab(Tab.Home);
      IPAmount.Content = Session.Current.Account.IP;
      RPAmount.Content = Session.Current.Account.RP;
      NameLabel.Content = Session.Current.Account.Name;
      ProfileIcon.Source = DataDragon.GetProfileIconImage(DataDragon.GetIconData(Session.Current.Account.ProfileIconID)).Load();
      LoLClient.PopupSelector = Popup;

      OpenChats.ItemsSource = OpenChatsList;
      Popup.IconSelector.IconSelected += IconSelector_IconSelected;

      Session.Current.ChatManager.FriendListChanged += ChatManager_FriendListChanged;
    }

    private void ChatManager_FriendListChanged(object sender, EventArgs e) {
      Dispatcher.Invoke(() => {
        FriendsList.ItemsSource = Session.Current.ChatManager.FriendList;
      });
    }

    private void ChatManager_MessageReceived(object sender, Message e) {
      if (!Session.Current.ChatManager.Friends.ContainsKey(e.From.User)) return;
      var friend = Session.Current.ChatManager.Friends[e.From.User];
      if (!OpenChatsList.Contains(friend)) Dispatcher.Invoke(() => OpenChatsList.Add(friend));
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

    private static readonly Duration slide = new Duration(TimeSpan.FromMilliseconds(200));

    private void Tab_Click(object sender, RoutedEventArgs e) {
      if (sender == LogoutTab) {
        Session.Current.Logout();
        LoLClient.MainWindow.Start();
      } else if (sender == PlayTab) ShowTab(Tab.Play);
      else if (sender == HomeTab) ShowTab(Tab.Home);
      else if (sender == ProfileTab) ShowTab(Tab.Profile);
      else if (sender == ShopTab) {
        //TODO RiotServices.LoginService.GetStoreUrl().ContinueWith(t => {
        //  System.Diagnostics.Process.Start(t.Result);
        //});
      }
    }

    private void ShowTab(Tab tab) {
      int pageHeight = (int) (double) FindResource("PageHeight");
      SlidingGrid.Height = pageHeight * Enum.GetValues(typeof(Tab)).Length;

      var slideAnim = new ThicknessAnimation(new Thickness(0, -pageHeight * ((int) tab), 0, 0), new Duration(TimeSpan.FromMilliseconds(100)));
      SlidingGrid.BeginAnimation(MarginProperty, slideAnim);

      if (tab == Tab.Play) PlayPage.Reset();
    }

    private void PlayTab_MouseEnter(object sender, MouseEventArgs e) {
      PlayTabGlow.Color = App.FocusColor;
    }

    private void PlayTab_MouseLeave(object sender, MouseEventArgs e) {
      PlayTabGlow.Color = Colors.Black;
    }
    #endregion

    #region Other Events

    private void Info_Popped(object sender, EventArgs e) {
      LoLClient.QueueManager.ShowInfo(null);
    }

    private void CurrentPopup_Close(object sender, QueueEventArgsa e) {
      Dispatcher.Invoke(() => PopupPanel.BeginStoryboard(App.FadeOut));
      CurrentPopup.Close -= CurrentPopup_Close;
      CurrentPopup = null;
    }

    private void Grid_MouseDown(object sender, MouseButtonEventArgs e) {
      if (e.ChangedButton == MouseButton.Left)
        if (e.ClickCount == 2) LoLClient.MainWindow.Center();
        else LoLClient.MainWindow.DragMove();
    }

    private void Friend_MouseUp(object sender, MouseButtonEventArgs e) {
      var item = (FriendListItem) sender;
      if (e.ChangedButton == MouseButton.Left && !OpenChatsList.Contains(((FriendListItem) sender).friend))
        OpenChatsList.Add(item.friend);
      else if (e.ChangedButton == MouseButton.Left) {
        var container = VisualTreeHelper.GetChild(OpenChats.ItemContainerGenerator.ContainerFromItem(item.friend), 0);
        ((ChatConversation) container).Open = true;
      }
    }

    private void ChatCombo_SelectionChanged(object sender, SelectionChangedEventArgs e) {
      Session.Current.ChatManager.ChatMode = (ChatMode) ChatCombo.SelectedItem;
    }

    private void ProfileIcon_Click(object sender, MouseButtonEventArgs e) {
      Popup.CurrentSelector = PopupSelector.Selector.ProfileIcons;
      Popup.BeginStoryboard(App.FadeIn);
    }

    private void IconSelector_IconSelected(object sender, Icon e) {
      Popup.BeginStoryboard(App.FadeOut);
      Session.Current.Account.SetSummonerIcon(e.IconId);
      ProfileIcon.Source = DataDragon.GetProfileIconImage(DataDragon.GetIconData(e.IconId)).Load();
    }

    private void Popup_Close(object sender, EventArgs e) {
      Popup.BeginStoryboard(App.FadeOut);
    }

    private void ChatConversation_ChatClosed(object sender, EventArgs e) {
      OpenChatsList.Remove(((ChatConversation) sender).friend);
    }

    private void ChatConversation_ChatOpened(object sender, EventArgs e) {
      foreach (var item in OpenChats.ItemContainerGenerator.Items) {
        var container = VisualTreeHelper.GetChild(OpenChats.ItemContainerGenerator.ContainerFromItem(item), 0);
        if (container != sender) ((ChatConversation) container).Open = false;
      }
    }

    private void Animate(Button butt, string key) => butt.BeginStoryboard((Storyboard) butt.FindResource(key));
    #endregion

    #region Interface

    void IQueueManager.ShowQueuePopup(IQueuePopup popup) {
      CurrentPopup = popup;
      CurrentPopup.Close += CurrentPopup_Close;

      PopupPanel.BeginStoryboard(App.FadeIn);
      PopupPanel.Child = popup.Control;
    }

    void IQueueManager.ShowPage() {
      if (Thread.CurrentThread != Dispatcher.Thread) { Dispatcher.Invoke(LoLClient.QueueManager.ShowPage); return; }

      ShowTab(Tab.Play);
    }

    void IQueueManager.ShowPage(IClientSubPage page) {
      if (Thread.CurrentThread != Dispatcher.Thread) { Dispatcher.MyInvoke(LoLClient.QueueManager.ShowPage, page); return; }

      CloseSubPage();
      page.Close += HandlePageClose;
      CurrentPage = page;
      SubPageArea.Content = page.Page;
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

    void IQueueManager.JoinLobby(Lobby lobby) {
      var tbd = lobby as TBDLobby;
      var def = lobby as QueueLobby;
      var custom = lobby as CustomLobby;

      IClientSubPage page;
      if (tbd != null) {
        page = new TBDLobbyPage(tbd);
      } else if (def != null) {
        page = new DefaultLobbyPage(def);
      } else if (custom != null) {
        page = new CustomLobbyPage(custom);
      } else
        throw new Exception("Unknown lobby type " + lobby);

      LoLClient.QueueManager.ShowPage(page);
    }

    //var task = RiotServices.GameInvitationService.Accept(invite.InvitationId);
    //var metaData = JSONParser.ParseObject(invite.GameMetaData, 0);
    //if ((int) metaData["gameTypeConfigId"] == 12) {
    //  var lobby = new CapLobbyPage(false);
    //  task.ContinueWith(t => lobby.GotLobbyStatus(task.Result));
    //  RiotServices.CapService.JoinGroupAsInvitee((string) metaData["groupFinderId"]);
    //  Session.Current.QueueManager.ShowPage(lobby);
    //} else {
    //  switch ((string) metaData["gameType"]) {
    //    case "PRACTICE_GAME":
    //      var custom = new CustomLobbyPage();
    //      task.ContinueWith(t => custom.GotLobbyStatus(task.Result));
    //      Session.Current.QueueManager.ShowPage(custom);
    //      break;
    //    case "NORMAL_GAME":
    //      var normal = new DefaultLobbyPage(new MatchMakerParams { QueueIds = new[] { (int) metaData["queueId"] } });
    //      task.ContinueWith(t => normal.GotLobbyStatus(task.Result));
    //      Session.Current.QueueManager.ShowPage(normal);
    //      break;
    //    case "RANKED_TEAM_GAME":
    //      var ranked = new DefaultLobbyPage(new MatchMakerParams { QueueIds = new[] { (int) metaData["queueId"] }, TeamId = new TeamId { FullId = (string) metaData["rankedTeamId"] } });
    //      task.ContinueWith(t => ranked.GotLobbyStatus(task.Result));
    //      Session.Current.QueueManager.ShowPage(ranked);
    //      break;
    //  }
    //}

    void IQueueManager.ViewProfile(string summonerName) {
      Session.Current.SummonerCache.GetData(summonerName, Profile.GotSummoner);
      Dispatcher.MyInvoke(ShowTab, Tab.Profile);
    }

    #endregion

    private void CloseSubPage() {
      if (Thread.CurrentThread != Dispatcher.Thread) { Dispatcher.Invoke(CloseSubPage); return; }

      if (CurrentPage != null) {
        CurrentPage.Close -= HandlePageClose;
        CurrentPage.Dispose();
        CurrentPage = null;
        SubPageArea.Content = null;
        SubPageArea.Visibility = Visibility.Collapsed;
        //ShowTab(Tab.Friends);
      }
    }

    private void HandlePageClose(object source, EventArgs e) {
      if (Thread.CurrentThread != Dispatcher.Thread)
        Dispatcher.Invoke(CloseSubPage);
      else
        CloseSubPage();
    }

    private enum Tab {
      Play = 0,
      Home = 1,
      Profile = 2
    }

    private void Page_KeyUp(object sender, KeyEventArgs e) {
      if (e.Key == Key.D && Keyboard.IsKeyDown(Key.LeftCtrl)) {
        //new DebugWindow().Show();
      }
    }
  }
}
