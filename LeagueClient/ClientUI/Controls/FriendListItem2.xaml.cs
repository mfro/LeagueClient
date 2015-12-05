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
using LeagueClient.Logic;
using LeagueClient.Logic.Chat;
using LeagueClient.Logic.Riot;
using MFroehlich.League.Assets;
using LeagueClient.Logic.Riot.Platform;
using MFroehlich.Parsing.DynamicJSON;

namespace LeagueClient.ClientUI.Controls {
  /// <summary>
  /// Interaction logic for FriendListItem2.xaml
  /// </summary>
  public partial class FriendListItem2 : UserControl {
    private const string key_Expand = "expand";
    private static readonly Duration Duration = new Duration(TimeSpan.FromMilliseconds(50));
    private static readonly DoubleAnimation ExpandWidth = new DoubleAnimation(120, Duration);
    private static readonly ThicknessAnimation ExpandMargin = new ThicknessAnimation(new Thickness(0, 4, 8, 4), Duration);
    private static readonly DoubleAnimation ContractWidth = new DoubleAnimation(0, Duration);
    private static readonly ThicknessAnimation ContractMargin = new ThicknessAnimation(new Thickness(0, 4, 0, 4), Duration);

    public ChatFriend friend { get; private set; }
    public bool ForceExpand {
      get { return !friend.Data.ContainsKey(key_Expand) || (bool) friend.Data[key_Expand]; }
      set {
        friend.Data[key_Expand] = value;
        if (value) {
          Sidebar.BeginAnimation(WidthProperty, ExpandWidth);
          Sidebar.BeginAnimation(MarginProperty, ExpandMargin);
        } else {
          Sidebar.BeginAnimation(WidthProperty, ContractWidth);
          Sidebar.BeginAnimation(MarginProperty, ContractMargin);
        }
      }
    }

    public FriendListItem2() {
      InitializeComponent();

      if (Client.Connected)
        Loaded += (src, e) => {
          Client.ChatManager.Tick += (src2, e2) => Dispatcher.Invoke(Update);
          friend = (ChatFriend) DataContext;
          Update();
        };
    }

    private void Invite_Click(object sender, RoutedEventArgs e) {
      RiotServices.GameInvitationService.Invite(friend.Summoner.SummonerId);
    }

    private void SizeToggle_Click(object sender, RoutedEventArgs e) {
      ForceExpand = !ForceExpand;
      if (ForceExpand) {
        ((MenuItem) sender).Header = "Contract";
      } else {
        ((MenuItem) sender).Header = "Expand";
      }
    }

    private void Update() {
      if (friend.Status == null) return;
      NameText.Content = friend.User.Nickname;
      MsgText.Text = friend.Status.Message;
      if (string.IsNullOrWhiteSpace(friend.Status.Message)) {
        MsgText.Visibility = Visibility.Collapsed;
      } else {
        MsgText.Visibility = Visibility.Visible;
      }
      SummonerIcon.Source = LeagueData.GetProfileIconImage(LeagueData.GetIconData(friend.Status.ProfileIcon));
      switch (friend.Status.Show) {
        case StatusShow.Chat: StatusText.Foreground = NameText.Foreground = App.ChatBrush; break;
        case StatusShow.Away: StatusText.Foreground = NameText.Foreground = App.AwayBrush; break;
        case StatusShow.Dnd: StatusText.Foreground = NameText.Foreground = App.BusyBrush; break;
      }

      TimeText.Visibility = ChampText.Visibility = Visibility.Collapsed;
      if (friend.CurrentGameDTO != null) {
        TimeText.Visibility = Visibility.Visible;
        if (friend.CurrentGameInfo == null) {
          long time = friend.Status.TimeStamp - Client.GetMilliseconds();
          TimeText.Content = TimeSpan.FromMilliseconds(time).ToString("m\\:ss");
        } else if (friend.CurrentGameInfo.gameStartTime == 0) {
          TimeText.Content = "Loading";
        } else {
          long time = friend.CurrentGameInfo.gameStartTime - Client.GetMilliseconds();
          TimeText.Content = TimeSpan.FromMilliseconds(time).ToString("m\\:ss");
        }
        StatusText.Content = QueueType.Values[friend.CurrentGameDTO.QueueTypeName].Value;
        if (!string.IsNullOrEmpty(friend.Status.Champion)) {
          ChampText.Visibility = Visibility.Visible;
          ChampText.Content = LeagueData.ChampData.Value.data[friend.Status.Champion].name;
        }
      } else StatusText.Content = friend.Status.GameStatus.Value;
      if (friend.Invite != null) {
        ChampText.Visibility = Visibility.Visible;
        ChampText.Content = "Invited you";
      }
    }

    private void This_DoubleClick(object sender, MouseButtonEventArgs e) {
      ForceExpand = !ForceExpand;
    }

    private void DeclineButt_Click(object sender, RoutedEventArgs e) {
      RiotServices.GameInvitationService.Decline(friend.Invite.InvitationId);
    }

    private void AcceptButt_Click(object sender, RoutedEventArgs e) {
      Client.QueueManager.AcceptInvite(friend.Invite);
      friend.Invite = null;
    }

    private void Champ_MouseEnter(object sender, MouseEventArgs e) {
      if(friend.Invite != null) {
        AcceptButt.BeginStoryboard(App.FadeIn);
        DeclineButt.BeginStoryboard(App.FadeIn);
      }
    }

    private void Champ_MouseLeave(object sender, MouseEventArgs e) {
      AcceptButt.BeginStoryboard(App.FadeOut);
      DeclineButt.BeginStoryboard(App.FadeOut);
    }
  }
}
