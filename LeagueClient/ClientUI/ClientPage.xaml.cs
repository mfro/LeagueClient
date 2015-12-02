using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
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
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using LeagueClient.ClientUI.Controls;
using LeagueClient.ClientUI.Main;
using LeagueClient.Logic;
using LeagueClient.Logic.Cap;
using LeagueClient.Logic.Chat;
using LeagueClient.Logic.Queueing;
using LeagueClient.Logic.Riot;
using LeagueClient.Logic.Riot.Platform;
using MFroehlich.League.Assets;
using MFroehlich.League.DataDragon;
using MFroehlich.Parsing.DynamicJSON;
using RtmpSharp.Messaging;

namespace LeagueClient.ClientUI {
  /// <summary>
  /// Interaction logic for ClientPage.xaml
  /// </summary>
  public partial class ClientPage : Page, IQueueManager, IClientPage {
    public string SummonerName { get; set; }
    public List<Alert> Alerts { get; } = new List<Alert>();
    public IClientSubPage CurrentPage { get; private set; }
    public bool ChatOpen {
      get { return chatOpen; }
      set {
        if (!value)
          ChatPanel.BeginStoryboard(App.FadeOut);
        else
          ChatPanel.BeginStoryboard(App.FadeIn);
        chatOpen = value;
      }
    }
    public bool AlertsOpen {
      get { return alertsOpen; }
      set {
        RecentAlert.Visibility = Visibility.Collapsed;
        RecentAlert.Opacity = 0;
        RecentAlert.Child = null;
        if (value) {
          PastAlerts.BeginStoryboard(App.FadeIn);
          var alerts = new List<UIElement>();
          foreach (var item in Alerts) alerts.Add(item.History);
          AlertHistory.ItemsSource = alerts;
        } else PastAlerts.BeginStoryboard(App.FadeOut);
        alertsOpen = value;
      }
    }

    private bool chatOpen; 
    private bool alertsOpen;
    private IQueuer CurrentQueuer;
    private IQueuePopup CurrentPopup;

    public ClientPage() {
      SummonerName = Client.LoginPacket.AllSummonerData.Summoner.Name;
      InitializeComponent();

      ProfileIcon.Source = LeagueData.GetProfileIconImage(LeagueData.GetIconData(Client.LoginPacket.AllSummonerData.Summoner.ProfileIconId));
      OpenChatList.ItemsSource = Client.ChatManager.OpenChats;
      IPAmount.Text = Client.LoginPacket.IpBalance.ToString();
      RPAmount.Text = Client.LoginPacket.RpBalance.ToString();

      //ChatList.ItemsSource = Client.ChatManager.FriendList;
      Client.ChatManager.FriendList.ListChanged += FriendList_ListChanged;
      Client.ChatManager.StatusUpdated += ChatManager_StatusUpdated;

      Popup.IconSelector.IconSelected += IconSelector_IconSelected;

      UpdatePlayButton();
    }

    private void FriendList_ListChanged(object sender, ListChangedEventArgs e) {
      ChatButt1.Content = ChatButt2.Content = $"Chat - {((System.Collections.IList) sender).Count}";
    }

    private void ChatManager_StatusUpdated(object sender, Logic.Chat.StatusUpdatedEventArgs e) {
      Dispatcher.Invoke(() => {
        switch (e.PresenceType) {
          case jabber.protocol.client.PresenceType.available:
            if (e.Status.GameStatus == ChatStatus.outOfGame) {
              if (!string.IsNullOrWhiteSpace(e.Status.Message))
                CurrentStatus.Text = e.Status.Message;
              else if (e.Show == StatusShow.Chat)
                CurrentStatus.Text = "Online";
              else CurrentStatus.Text = "Away";
            } else CurrentStatus.Text = e.Status.GameStatus.Value;

            switch (e.Show) {
              case StatusShow.Away: CurrentStatus.Foreground = App.AwayBrush; break;
              case StatusShow.Chat: CurrentStatus.Foreground = App.ChatBrush; break;
              case StatusShow.Dnd: CurrentStatus.Foreground = App.BusyBrush; break;
            }

            break;
          case jabber.protocol.client.PresenceType.invisible:
            CurrentStatus.Text = "Invisible";
            CurrentStatus.Foreground = App.ForeBrush;
            break;
        }
      });
    }

    public bool HandleMessage(MessageReceivedEventArgs args) {
      if (CurrentPage?.HandleMessage(args) ?? false) return true;
      if (CurrentQueuer?.HandleMessage(args) ?? false) return true;
      return CurrentPopup?.HandleMessage(args) ?? false;
    }

    #region IQueueManager

    public void ShowQueuer(IQueuer queuer) {
      if (Thread.CurrentThread != Dispatcher.Thread) { Dispatcher.MyInvoke(ShowQueuer, queuer); return; }

      CurrentQueuer = queuer;
      queuer.Popped += Queue_Popped;
      StatusPanel.Child = queuer.GetControl();
      UpdatePlayButton();
    }

    public void ShowNotification(Alert alert) {
      if (Thread.CurrentThread != Dispatcher.Thread) { Dispatcher.MyInvoke(ShowNotification, alert); return; }

      Alerts.Add(alert);
      alert.Close += Alert_Close; ;
      RecentAlert.Child = alert.Popup;
      AlertButt.BeginStoryboard(App.FadeIn);

      RecentAlert.BeginStoryboard(App.FadeIn);
      new Thread(() => {
        System.Threading.Thread.Sleep(3500);
        Dispatcher.MyInvoke(RecentAlert.BeginStoryboard, App.FadeOut);
      }).Start();
    }

    public void ShowPage(IClientSubPage page) {
      if (Thread.CurrentThread != Dispatcher.Thread) { Dispatcher.MyInvoke(ShowPage, page); return; }

      CloseSubPage(true);
      page.Close += HandlePageClose;
      CurrentPage = page;
      SubPage.Content = page?.Page;
      UpdatePlayButton();
    }

    public void BeginChampionSelect(GameDTO game) {
      var page = new ChampSelectPage(game);
      ShowPage(page);
      Client.ChatManager.UpdateStatus(ChatStatus.championSelect);
      RiotServices.GameService.SetClientReceivedGameMessage(game.Id, "CHAMP_SELECT_CLIENT");
    }

    #endregion

    private void CloseSubPage(bool notifyPage) {
      if (Thread.CurrentThread != Dispatcher.Thread) { Dispatcher.MyInvoke(CloseSubPage, notifyPage); return; }

      if (CurrentPage != null) {
        CurrentPage.Close -= HandlePageClose;
        if (notifyPage) {
          var x = CurrentPage.HandleClose();
          if (x != null) ShowQueuer(x);
        }
        SubPage.Content = null;
        CurrentPage = null;
      }
      UpdatePlayButton();
    }

    private void HandlePageClose(object source, EventArgs e) {
      if (Thread.CurrentThread != Dispatcher.Thread)
        Dispatcher.MyInvoke(CloseSubPage, false);
      else
        CloseSubPage(false);
    }

    private void ShowPopup(Control contents) {
      if (Thread.CurrentThread != Dispatcher.Thread) { Dispatcher.MyInvoke(ShowPopup, contents); return; }

      PopupPanel.Visibility = Visibility.Visible;
      PopupPanel.Child = contents;
      contents.Opacity = 0;
      contents.BeginStoryboard(App.FadeIn);
    }

    private void ToggleChat(object sender, RoutedEventArgs e) {
      if (Thread.CurrentThread != Dispatcher.Thread) { Dispatcher.MyInvoke(ToggleChat, sender, e); return; }

      ChatOpen = !chatOpen;
    }

    private void CloseStuff() {
      ChatOpen = false;
      AlertsOpen = false;
      Client.ChatManager.CloseAll();
    }

    private void UpdatePlayButton() {
      PlayButton.IsEnabled = (CurrentPage == null || CurrentPage.CanPlay) && CurrentQueuer == null && CurrentPopup == null;
      if (CurrentPage == null) {
        HomeButt.Content = "Logout";
      } else {
        HomeButt.IsEnabled = CurrentPage.CanClose;
        HomeButt.Content = "Home";
      }
    }

    #region Queue Related Event Listeners
    private void Queue_Popped(object src, QueuePoppedEventArgs args) {
      if (Thread.CurrentThread != Dispatcher.Thread) { Dispatcher.MyInvoke(Queue_Popped, src, args); return; }

      StatusPanel.Child = null;
      CurrentQueuer = null;
      UpdatePlayButton();
      if (args.QueuePopup == null) return;
      CurrentPopup = args.QueuePopup;
      UpdatePlayButton();
      CurrentPopup.Close += QueuePopupClose;
      ShowPopup(CurrentPopup.Control);
    }

    private void QueuePopupClose(object src, EventArgs args) {
      if (Thread.CurrentThread != Dispatcher.Thread) { Dispatcher.MyInvoke(QueuePopupClose, src, args); return; }

      CurrentPopup = null;
      UpdatePlayButton();
      PopupPanel.Visibility = Visibility.Collapsed;
    }

    private void StatusBox_KeyUp(object sender, KeyEventArgs e) {
      if (e.Key != Key.Enter) return;
      Client.ChatManager.UpdateStatus(StatusBox.Text);
    }

    private void Play_Click(object sender, RoutedEventArgs e) {
      PlayButton.IsEnabled = false;
      CloseStuff();
      //ShowPage(new PlaySelectPage());
    }

    private void Home_Click(object sender, RoutedEventArgs e) {
      PlayControl.Child = PlayButton;
      if (CurrentPage == null) {
        Client.Logout();
      } else CloseSubPage(true);
    }
    #endregion

    #region Other Event Listeners

    private void Page_KeyUp(object sender, KeyEventArgs e) {
      if (e.Key == Key.Escape) { CloseStuff(); }
    }

    private void AlertButt_Click(object sender, RoutedEventArgs e) {
      AlertsOpen = !alertsOpen;
    }

    private void Alert_Close(object sender, bool removeFromHistory) {
      RecentAlert.BeginStoryboard(App.FadeOut);
      if (removeFromHistory)
        Alerts.Remove(sender as Alert);
      if (Alerts.Count == 0) {
        AlertsOpen = false;
        AlertButt.BeginStoryboard(App.FadeOut);
      }
    }

    bool canDouble;
    private void Header_MouseDown(object sender, MouseButtonEventArgs e) {
      if (e.ClickCount == 2 && canDouble) Client.MainWindow.Center();
    }

    private void Header_MouseMove(object sender, MouseEventArgs e) {
      if (e.LeftButton == MouseButtonState.Pressed) {
        Client.MainWindow.DragMove();
        canDouble = false;
      } else {
        canDouble = true;
      }
    }

    private void CurrentStatus_MouseUp(object sender, MouseButtonEventArgs e) {
      switch (Client.ChatManager.Show) {
        case StatusShow.Away:
          Client.ChatManager.UpdateStatus(StatusShow.Chat);
          break;
        case StatusShow.Chat:
          Client.ChatManager.UpdateStatus(StatusShow.Away);
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

    #endregion
  }
}
