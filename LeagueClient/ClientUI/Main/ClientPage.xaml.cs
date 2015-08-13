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
using LeagueClient.Logic;
using LeagueClient.Logic.Cap;
using LeagueClient.Logic.Queueing;
using LeagueClient.Logic.Riot;
using LeagueClient.Logic.Riot.Platform;
using MFroehlich.League.Assets;
using MFroehlich.League.DataDragon;
using MFroehlich.Parsing.DynamicJSON;

namespace LeagueClient.ClientUI.Main {
  /// <summary>
  /// Interaction logic for ClientPage.xaml
  /// </summary>
  public partial class ClientPage : Page, IQueueManager {
    public BitmapImage ProfileIcon { get; set; }
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
          foreach (var item in Alerts) alerts.Add(item.Control);
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
      int iconId = Client.LoginPacket.AllSummonerData.Summoner.ProfileIconId;
      ProfileIcon = LeagueData.GetProfileIconImage(iconId);
      SummonerName = Client.LoginPacket.AllSummonerData.Summoner.Name;
      InitializeComponent();
      OpenChatList.ItemsSource = Client.ChatManager.OpenChats;
      IPAmount.Text = Client.LoginPacket.IpBalance.ToString();
      RPAmount.Text = Client.LoginPacket.RpBalance.ToString();

      Client.ChatManager.ChatListUpdated += ChatManager_ChatListUpdated;
    }

    private void ChatManager_ChatListUpdated(object sender, IEnumerable<Friend> e) {
      ChatList.ItemsSource = e;
    }

    #region IQueueManager
    //public void CreateCapSolo() {
    //  var page = new CapSoloPage();
    //  ShowPage(page);
    //}

    //public void CreateCapLobby() {
    //  var page = new CapLobbyPage();
    //  ShowPage(page);
    //  RiotCalls.CapService.CreateGroup();
    //}

    //public void JoinCapLobby(Logic.Cap.CapPlayer player) {
    //  var page = new CapLobbyPage(player);
    //  ShowPage(page);
    //}

    //public void JoinCapLobby(Task<LobbyStatus> pending) {
    //  var page = new CapLobbyPage();
    //  ShowPage(page);
    //  pending.ContinueWith(t => page.GotLobbyStatus(t.Result));
    //}

    private bool CheckInvoke(Delegate callback, params object[] args) {
      if (Thread.CurrentThread != Dispatcher.Thread) {
        Dispatcher.Invoke(callback, args);
        return true;
      }
      return false;
    }

    private bool CheckInvoke<T>(Action<T> callback, T t) {
      if (Thread.CurrentThread != Dispatcher.Thread) {
        Dispatcher.Invoke(callback, t);
        return true;
      }
      return false;
    }

    private bool CheckInvoke<T1, T2>(Action<T1, T2> callback, T1 t1, T2 t2) {
      if (Thread.CurrentThread != Dispatcher.Thread) {
        Dispatcher.Invoke(callback, t1, t2);
        return true;
      }
      return false;
    }

    public void ShowQueuer(IQueuer Queuer) {
      if (CheckInvoke(ShowQueuer, Queuer)) return;

      CurrentQueuer = Queuer;
      Queuer.Popped += Queue_Popped;
      StatusPanel.Child = Queuer.GetControl();
      UpdatePlayButton();
    }

    public void ShowNotification(Alert alert) {
      if (CheckInvoke(ShowNotification, alert)) return;

      Alerts.Add(alert);
      alert.Handled += Alert_Handled;
      RecentAlert.Child = alert.Control;
      AlertButt.BeginStoryboard(App.FadeIn);

      RecentAlert.BeginStoryboard(App.FadeIn);
      Task.Run(() => {
        System.Threading.Thread.Sleep(3500);
        Dispatcher.Invoke(() => RecentAlert.BeginStoryboard(App.FadeOut));
      });
    }

    public void ShowPage(IClientSubPage page) {
      if (CheckInvoke(ShowPage, page)) return;

      CloseSubPage(true);
      page.Close += HandlePageClose;
      CurrentPage = page;
      SubPage.Content = page?.GetPage();
      UpdatePlayButton();
    }
    #endregion

    private void CloseSubPage(bool notifyPage) {
      if (CheckInvoke(CloseSubPage, notifyPage)) return;

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
      if (CheckInvoke(HandlePageClose, source, e)) return;

      CloseSubPage(false);
    }

    private void ShowPopup(Control contents) {
      if (CheckInvoke(ShowPopup, contents)) return;

      PopupPanel.Visibility = Visibility.Visible;
      PopupPanel.Child = contents;
      contents.Opacity = 0;
      contents.BeginStoryboard(App.FadeIn);
    }

    private void ToggleChat(object sender, RoutedEventArgs e) {
      if (CheckInvoke(ToggleChat, sender, e)) return;

      ChatOpen = !chatOpen;
    }

    private void CloseStuff() {
      ChatOpen = false;
      AlertsOpen = false;
      Client.ChatManager.CloseAll();
    }

    private void UpdatePlayButton() {
      PlayButton.IsEnabled = (CurrentPage == null || CurrentPage.CanPlay()) && CurrentQueuer == null && CurrentPopup == null;
    }

    #region Queue Related Event Listeners
    private void Queue_Popped(object src, QueuePoppedEventArgs args) {
      if (CheckInvoke(Queue_Popped, src, args)) return;

      StatusPanel.Child = null;
      CurrentQueuer = null;
      UpdatePlayButton();
      if (args.QueuePopup == null) return;
      CurrentPopup = args.QueuePopup;
      UpdatePlayButton();
      CurrentPopup.Accepted += QueuePopupClose;
      CurrentPopup.Cancelled += QueuePopupClose;
      ShowPopup(CurrentPopup.GetControl());
    }

    private void QueuePopupClose(object src, EventArgs args) {
      if (CheckInvoke(QueuePopupClose, src, args)) return;

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
      ShowPage(new PlaySelectPage());
    }

    private void Home_Click(object sender, RoutedEventArgs e) {
      PlayControl.Child = PlayButton;
      if (CurrentQueuer == null)
        PlayButton.IsEnabled = true;
      if (CurrentPage == null)
        ShowPage(new DebugPage());
      else CloseSubPage(true);
    }
    #endregion

    #region Other Event Listeners
    private void Page_KeyUp(object sender, KeyEventArgs e) {
      if (e.Key == Key.Escape) { CloseStuff(); }
    }

    private void AlertButt_Click(object sender, RoutedEventArgs e) {
      AlertsOpen = !alertsOpen;
    }

    private void Alert_Handled(object sender, AlertEventArgs e) {
      Alerts.Remove(sender as Alert);
      if(Alerts.Count == 0) {
        AlertsOpen = false;
        AlertButt.BeginStoryboard(App.FadeOut);
      }
    }

    private void Header_MouseDown(object sender, MouseButtonEventArgs e) {
      if (e.ChangedButton == MouseButton.Left) Client.MainWindow.DragMove();
    }
    #endregion
  }
}
