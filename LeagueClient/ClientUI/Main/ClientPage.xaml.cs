﻿using System;
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
using static LeagueClient.Logic.Strings;

namespace LeagueClient.ClientUI.Main {
  /// <summary>
  /// Interaction logic for ClientPage.xaml
  /// </summary>
  public partial class ClientPage : Page, IQueueManager {
    public BitmapImage ProfileIcon { get; set; }
    public string SummonerName { get; set; }
    public BindingList<Alert> Alerts { get; private set; }
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
        if (value)
          AlertPopup.BeginStoryboard(App.FadeIn);
        else AlertPopup.BeginStoryboard(App.FadeOut);
        alertsOpen = value;
      }
    }

    private bool chatOpen;
    private bool alertsOpen;
    private Button PlayButton = new Button { Content = "Play", FontSize = 18 };

    public ClientPage() {
      PlayButton.Click += Play_Click;
      
      int iconId = Client.LoginPacket.AllSummonerData.Summoner.ProfileIconId;
      ProfileIcon = LeagueData.GetProfileIconImage(iconId);
      SummonerName = Client.LoginPacket.AllSummonerData.Summoner.Name;
      Alerts = new BindingList<Alert>();
      InitializeComponent();
      PlayControl.Child = PlayButton;
      ChatList.ItemsSource = Client.ChatManager.FriendList;
      OpenChatList.ItemsSource = Client.ChatManager.OpenChats;
      IPAmount.Text = Client.LoginPacket.IpBalance.ToString();
      RPAmount.Text = Client.LoginPacket.RpBalance.ToString();
    }

    public void JoinQueue(GameQueueConfig queue, string bots) {
      Client.Log("");
      foreach (var field in queue.GetType().GetProperties()) {
        Client.Log(field.Name + ": " + field.GetValue(queue));
      }
      //TODO Queueing
    }

    public void CreateLobby(GameQueueConfig queue, string bots) {

    }

    public void CreateCapSolo() {
      var page = new CapSoloPage();
      ShowSubPage(page);
    }

    public void CreateCapLobby() {
      var page = new CapLobbyPage();
      ShowSubPage(page);
      RiotCalls.CapService.CreateGroup();
    }

    public void JoinCapLobby(string groupId, int slotId, CapMePlayer player) {
      var page = new CapLobbyPage(slotId, player);
      ShowSubPage(page);
      RiotCalls.CapService.IndicateGroupAcceptanceAsCandidate(slotId, true, groupId);
    }

    public void ShowQueuer(IQueuer Queuer) {
      Queuer.Popped += (src, e) => Dispatcher.Invoke(() => StatusPanel.Child = null);
      StatusPanel.Child = Queuer.GetControl();
    }

    public void ShowQueuePopPopup(IQueuePopup popup) {
      popup.Accepted += QueuePopupClose;
      popup.Cancelled += QueuePopupClose;
      ShowPopup(popup.GetControl());
    }

    public void ShowNotification(Alert alert) {
      Dispatcher.Invoke(() => {
        Alerts.Add(alert);
        AlertButton.BeginStoryboard(App.FadeIn);
      });
    }

    private void ShowSubPage(IClientSubPage page) {
      Dispatcher.Invoke(() => {
        PlayButton.IsEnabled = page == null || page.CanPlay();
        SubPage.Content = page?.GetPage();
      });
      page.Close += (s, e) => Dispatcher.Invoke(CloseSubPage);
    }

    public void CloseSubPage() {
      SubPage.Content = null;
    }

    private void QueuePopupClose(object src, EventArgs arg) {
      Dispatcher.Invoke(() => PopupPanel.Visibility = Visibility.Collapsed);
    }

    private void ShowPopup(Control Contents) {
      PopupPanel.Visibility = Visibility.Visible;
      PopupPanel.Child = Contents;
      Contents.Opacity = 0;
      Contents.BeginStoryboard(App.FadeIn);
    }

    private void ToggleChat(object sender, RoutedEventArgs e) {
      ChatOpen = !chatOpen;
    }

    private void StatusBox_KeyUp(object sender, KeyEventArgs e) {
      if (e.Key != Key.Enter) return;
      Client.ChatManager.UpdateStatus(StatusBox.Text);
    }

    private void Play_Click(object sender, RoutedEventArgs e) {
      PlayButton.IsEnabled = false;
      ShowSubPage(new PlaySelectPage());
    }

    private void Home_Click(object sender, RoutedEventArgs e) {
      PlayControl.Child = PlayButton;
      PlayButton.IsEnabled = true;
      CloseSubPage();
    }

    private void Page_KeyUp(object sender, KeyEventArgs e) {
      if(e.Key == Key.Escape) {
        ChatOpen = false;
        AlertsOpen = false;
        Client.ChatManager.CloseAll();
      }
    }

    private void AlertButton_Click(object sender, RoutedEventArgs e) {
      AlertsOpen = !alertsOpen;
    }

    private void AlertYes_Click(object sender, RoutedEventArgs e) {
      var alert = (sender as Button).DataContext as Alert;
      alert.ReactYesNo(true);
      Alerts.Remove(alert);
      if(Alerts.Count == 0) {
        AlertButton.BeginStoryboard(App.FadeOut);
        AlertsOpen = false;
      }
    }

    private void AlertNo_Click(object sender, RoutedEventArgs e) {
      var alert = (sender as Button).DataContext as Alert;
      alert.ReactYesNo(false);
      Alerts.Remove(alert);
      if (Alerts.Count == 0) {
        AlertButton.BeginStoryboard(App.FadeOut);
        AlertsOpen = false;
      }
    }
  }
}