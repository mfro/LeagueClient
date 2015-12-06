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
using LeagueClient.Logic;
using LeagueClient.Logic.Chat;
using LeagueClient.Logic.Riot;
using LeagueClient.Logic.Riot.Platform;
using MFroehlich.League.Assets;
using MFroehlich.League.RiotAPI;
using agsXMPP.protocol.client;

namespace LeagueClient.ClientUI.Controls {
  /// <summary>
  /// Interaction logic for Friend.xaml
  /// </summary>
  public partial class FriendListItem : UserControl {
    private ChatFriend friend;

    public FriendListItem() {
      InitializeComponent();
    }

    public FriendListItem(ChatFriend friend) : this() {
      this.friend = friend;

      Update();
    }

    public void Update() {
      if (friend.Status == null) return;
      NameText.Text = friend.User.Name;
      MsgText.Text = friend.Status.Message;
      if (string.IsNullOrWhiteSpace(friend.Status.Message)) {
        MsgText.Visibility = Visibility.Collapsed;
      } else {
        MsgText.Visibility = Visibility.Visible;
      }
      SummonerIcon.Source = LeagueData.GetProfileIconImage(LeagueData.GetIconData(friend.Status.ProfileIcon));
      switch (friend.Status.Show) {
        case ShowType.chat: InGameText.Foreground = App.ChatBrush; break;
        case ShowType.away: InGameText.Foreground = App.AwayBrush; break;
        case ShowType.dnd: InGameText.Foreground = App.BusyBrush; break;
      }

      string status;
      if (friend.CurrentGameDTO != null) {
        if (friend.CurrentGameInfo == null) {
          long time = friend.Status.TimeStamp - Client.GetMilliseconds();
          status = $"In {QueueType.Values[friend.CurrentGameDTO.QueueTypeName]} for ~{TimeSpan.FromMilliseconds(time).ToString("m\\:ss")}";
        } else if (friend.CurrentGameInfo.gameStartTime == 0) {
          status = "Loading into " + QueueType.Values[friend.CurrentGameDTO.QueueTypeName];
        } else {
          long time = friend.CurrentGameInfo.gameStartTime - Client.GetMilliseconds();
          status = $"In {QueueType.Values[friend.CurrentGameDTO.QueueTypeName]} for {TimeSpan.FromMilliseconds(time).ToString("m\\:ss")}";
        }
      } else status = friend.Status.GameStatus.Value;
      InGameText.Text = status;
    }

    private void MenuItem_Click(object sender, RoutedEventArgs e) {
      RiotServices.GameInvitationService.Invite(RiotChat.GetSummonerId(friend.User.Jid));
    }

    private void UserControl_MouseUp(object sender, MouseButtonEventArgs e) {
      if(e.ChangedButton == MouseButton.Right) {
        InviteButton.IsEnabled = Client.CanInviteFriends;
      }
    }
  }
}
