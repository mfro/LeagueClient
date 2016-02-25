﻿using agsXMPP.protocol.client;
using LeagueClient.Logic;
using LeagueClient.Logic.Chat;
using LeagueClient.Logic.Queueing;
using LeagueClient.Logic.Riot;
using LeagueClient.Logic.Riot.Platform;
using LeagueClient.Logic.Riot.Team;
using LeagueClient.UI.Main.Alerts;
using LeagueClient.UI.Main.Cap;
using LeagueClient.UI.Main.Custom;
using LeagueClient.UI.Main.Friends;
using LeagueClient.UI.Main.Lobbies;
using LeagueClient.UI.Selectors;
using MFroehlich.League.Assets;
using MFroehlich.League.RiotAPI;
using MFroehlich.Parsing.JSON;
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

namespace LeagueClient.UI.Main {
  /// <summary>
  /// Interaction logic for LandingPage.xaml
  /// </summary>
  public sealed partial class LandingPage : Page, IClientPage, IQueueManager {
    public IQueueInfo CurrentInfo { get; private set; }
    public IQueuePopup CurrentPopup { get; private set; }
    public IClientSubPage CurrentPage { get; private set; }
    public BindingList<ChatFriend> OpenChatsList { get; } = new BindingList<ChatFriend>();

    public LandingPage() {
      InitializeComponent();

      Client.Session.ChatManager.StatusUpdated += ChatManager_StatusUpdated;
      Client.Session.ChatManager.MessageReceived += ChatManager_MessageReceived;

      ChatCombo.ItemsSource = Enum.GetValues(typeof(ChatMode));
      ChatCombo.SelectedItem = ChatMode.Chat;

      ShowTab(Tab.Home);
      IPAmount.Content = Client.Session.LoginPacket.IpBalance.ToString();
      RPAmount.Content = Client.Session.LoginPacket.RpBalance.ToString();
      NameLabel.Content = Client.Session.LoginPacket.AllSummonerData.Summoner.Name;
      ProfileIcon.Source = DataDragon.GetProfileIconImage(DataDragon.GetIconData(Client.Session.LoginPacket.AllSummonerData.Summoner.ProfileIconId)).Load();
      Client.PopupSelector = Popup;

      OpenChats.ItemsSource = OpenChatsList;
      Popup.IconSelector.IconSelected += IconSelector_IconSelected;
      FriendsList.ItemsSource = Client.Session.ChatManager.FriendList;
    }

    private void ChatManager_MessageReceived(object sender, Message e) {
      if (!Client.Session.ChatManager.Friends.ContainsKey(e.From.User)) return;
      var friend = Client.Session.ChatManager.Friends[e.From.User];
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
    //private bool forceExpand;
    private static readonly Duration slide = new Duration(TimeSpan.FromMilliseconds(200));

    private void Tab_Click(object sender, RoutedEventArgs e) {
      //if (sender == PlayTab && Keyboard.IsKeyDown(Key.LeftShift)) {
      //  forceExpand = !forceExpand;
      //} else 
      if (sender == LogoutTab) Client.Session.Logout();
      else if (sender == PlayTab) ShowTab(Tab.Play);
      else if (sender == HomeTab) ShowTab(Tab.Home);
      else if (sender == ProfileTab) ShowTab(Tab.Profile);
      else if (sender == ShopTab) {
        RiotServices.LoginService.GetStoreUrl().ContinueWith(t => {
          System.Diagnostics.Process.Start(t.Result);
        });
      }

      //if (!forceExpand) {
      //  var shrink = new ThicknessAnimation(new Thickness(0), slide);
      //  PlayExpandUp.BeginAnimation(MarginProperty, shrink);
      //  PlayExpandDown.BeginAnimation(MarginProperty, shrink);
      //}
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
      //var top = new ThicknessAnimation(new Thickness(0, -65, 0, 0), slide);
      //PlayExpandUp.BeginAnimation(MarginProperty, top);
      //var bot = new ThicknessAnimation(new Thickness(0, 0, 0, -120), slide);
      //PlayExpandDown.BeginAnimation(MarginProperty, bot);
    }

    private void PlayTab_MouseLeave(object sender, MouseEventArgs e) {
      PlayTabGlow.Color = Colors.Black;
    }

    private void Border_MouseLeave(object sender, MouseEventArgs e) {
      //if (!forceExpand) {
      //  var shrink = new ThicknessAnimation(new Thickness(0), slide);
      //  PlayExpandUp.BeginAnimation(MarginProperty, shrink);
      //  PlayExpandDown.BeginAnimation(MarginProperty, shrink);
      //}
    }
    #endregion

    #region Other Events

    private void Info_Popped(object sender, EventArgs e) {
      Client.Session.QueueManager.ShowInfo(null);
    }

    private void CurrentPopup_Close(object sender, QueueEventArgs e) {
      Dispatcher.Invoke(() => PopupPanel.BeginStoryboard(App.FadeOut));
      CurrentPopup.Close -= CurrentPopup_Close;
      CurrentPopup = null;
    }

    private void Grid_MouseDown(object sender, MouseButtonEventArgs e) {
      if (e.ChangedButton == MouseButton.Left)
        if (e.ClickCount == 2) Client.MainWindow.Center();
        else Client.MainWindow.DragMove();
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
      Client.Session.ChatManager.ChatMode = (ChatMode) ChatCombo.SelectedItem;
    }

    private void ProfileIcon_Click(object sender, MouseButtonEventArgs e) {
      Popup.CurrentSelector = PopupSelector.Selector.ProfileIcons;
      Popup.BeginStoryboard(App.FadeIn);
    }

    private void IconSelector_IconSelected(object sender, Icon e) {
      Popup.BeginStoryboard(App.FadeOut);
      Client.Session.LoginPacket.AllSummonerData.Summoner.ProfileIconId = e.IconId;
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
      if (Thread.CurrentThread != Dispatcher.Thread) { Dispatcher.Invoke(Client.Session.QueueManager.ShowPage); return; }

      ShowTab(Tab.Play);
    }

    void IQueueManager.ShowPage(IClientSubPage page) {
      if (Thread.CurrentThread != Dispatcher.Thread) { Dispatcher.MyInvoke(Client.Session.QueueManager.ShowPage, page); return; }

      CloseSubPage();
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

    bool IQueueManager.AttachToQueue(SearchingForMatchNotification result) {
      if (result.PlayerJoinFailures != null) {
        var leaver = result.PlayerJoinFailures[0];
        bool me = leaver.Summoner.SummonerId == Client.Session.LoginPacket.AllSummonerData.Summoner.SummonerId;
        switch (leaver.ReasonFailed) {
          case "LEAVER_BUSTER":
            //TODO Leaverbuster
            break;
          case "QUEUE_DODGER":
            Dispatcher.Invoke(() => Client.Session.QueueManager.ShowInfo(new BingeQueuer(leaver.PenaltyRemainingTime, me ? null : leaver.Summoner.Name)));
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
        Client.Session.QueueManager.ShowPage(lobby);
      } else {
        switch ((string) metaData["gameType"]) {
          case "PRACTICE_GAME":
            var custom = new CustomLobbyPage();
            task.ContinueWith(t => custom.GotLobbyStatus(task.Result));
            Client.Session.QueueManager.ShowPage(custom);
            break;
          case "NORMAL_GAME":
            var normal = new DefaultLobbyPage(new MatchMakerParams { QueueIds = new[] { (int) metaData["queueId"] } });
            task.ContinueWith(t => normal.GotLobbyStatus(task.Result));
            Client.Session.QueueManager.ShowPage(normal);
            break;
          case "RANKED_TEAM_GAME":
            var ranked = new DefaultLobbyPage(new MatchMakerParams { QueueIds = new[] { (int) metaData["queueId"] }, TeamId = new TeamId { FullId = (string) metaData["rankedTeamId"] } });
            task.ContinueWith(t => ranked.GotLobbyStatus(task.Result));
            Client.Session.QueueManager.ShowPage(ranked);
            break;
        }
      }
    }

    void IQueueManager.ViewProfile(string summonerName) {
      Client.Session.SummonerCache.GetData(summonerName, Profile.GotSummoner);
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
