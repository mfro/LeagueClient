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
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using LeagueClient.Logic;
using MFroehlich.League.Assets;
using RiotClient.Lobbies;
using RiotClient;

namespace LeagueClient.UI.Client.Lobbies {
  /// <summary>
  /// Interaction logic for LobbyPlayer2.xaml
  /// </summary>
  public partial class LobbyPlayer2 : UserControl {
    public bool CanControl { get; set; }

    public QueueLobbyMember Member { get; }

    public LobbyPlayer2(bool amCaptain, QueueLobbyMember member, int profileIconId) {
      InitializeComponent();

      KickButton.Visibility = GiveInviteButt.Visibility = Visibility.Collapsed;
      CanControl = amCaptain;
      NameLabel.Content = member.Name;
      Member = member;

      ProfileIconImage.Source = DataDragon.GetProfileIconImage(DataDragon.GetIconData(profileIconId)).Load();

      member.Changed += (s, e) => Update();
      Update();
      Session.Current.SummonerCache.GetData(member.Name, GotSummoner);
    }

    private void Update() {
      Dispatcher.Invoke(() => {
        PlusPath.Visibility = Member.HasInvitePower ? Visibility.Collapsed : Visibility.Visible;
      });
    }

    private void UserControl_MouseEnter(object sender, MouseEventArgs e) {
      if (CanControl && !Member.IsMe)
        KickButton.Visibility = GiveInviteButt.Visibility = Visibility.Visible;
    }

    private void UserControl_MouseLeave(object sender, MouseEventArgs e) {
      KickButton.Visibility = GiveInviteButt.Visibility = Visibility.Collapsed;
    }

    private void GotSummoner(SummonerCache.Item item) {
      Dispatcher.Invoke(() => {
        if (item.Data != null) {
          ProfileIconImage.Source = DataDragon.GetProfileIconImage(DataDragon.GetIconData(item.Data.Summoner.ProfileIconId)).Load();
          RankLabel.Content = "Level " + item.Data.SummonerLevel.Level;
          NameLabel.Content = item.Data.Summoner.Name;
        }

        if (item.Leagues != null) {
          var league = item.Leagues.SummonerLeagues.FirstOrDefault(l => l.Queue.Equals(QueueType.RANKED_SOLO_5x5.Key));
          if (league != null) RankLabel.Content = RankedTier.Values[league.Tier] + " " + league.Rank;
        }
      });
    }

    private void Kick_Click(object sender, RoutedEventArgs e) {
      Member.Kick();
    }

    private void GiveInvite_Click(object sender, RoutedEventArgs e) {
      Member.GiveInvitePowers(!Member.HasInvitePower);
    }
  }
}
