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
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using LeagueClient.RiotInterface.Riot;
using LeagueClient.RiotInterface.Riot.Platform;
using MFroehlich.League.Assets;

namespace LeagueClient.ClientUI {
  /// <summary>
  /// Interaction logic for ClientPage.xaml
  /// </summary>
  public partial class ClientPage : Page {
    public BitmapImage ProfileIcon { get; set; }
    public string SummonerName { get; set; }
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
    private bool chatOpen;
    private Button PlayButton = new Button { Content = "Play" };

    public ClientPage() {
      PlayButton.FontSize = 18;
      PlayButton.Click += Play_Click;

      int iconId = Client.LoginPacket.AllSummonerData.Summoner.ProfileIconId;
      ProfileIcon = LeagueData.GetProfileIconImage(iconId);
      SummonerName = Client.LoginPacket.AllSummonerData.Summoner.Name;
      InitializeComponent();
      PlayControl.Content = PlayButton;
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

    public void JoinTeambuilder(bool group) {
      if (group) {
        ClientContent.Content = new TeambuilderLobbyPage();
      } else {
        ClientContent.Content = new TeambuilderSoloPage();
      }
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
      ClientContent.Content = new PlaySelectPage();
    }

    private void Home_Click(object sender, RoutedEventArgs e) {
      PlayControl.Content = PlayButton;
      PlayButton.IsEnabled = true;
      ClientContent.Content = null;
    }
  }
}
