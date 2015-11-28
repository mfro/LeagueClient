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
using LeagueClient.Logic.Riot;
using LeagueClient.Logic.Riot.Platform;
using MFroehlich.League.Assets;

namespace LeagueClient.ClientUI.Controls {
  /// <summary>
  /// Interaction logic for LobbyPlayer2.xaml
  /// </summary>
  public partial class LobbyPlayer2 : UserControl {
    public event EventHandler KickClicked;
    public event EventHandler GiveInviteClicked;

    private bool amCaptain;
    public long SummonerId;
    public string SummonerName;

    public LobbyPlayer2() { InitializeComponent(); }

    public LobbyPlayer2(bool amCaptain, Member member) {
      RiotServices.SummonerService.GetSummonerByName(member.SummonerName).ContinueWith(GotSummonerData);
      InitializeComponent();
      KickButton.Visibility = GiveInviteButt.Visibility = Visibility.Collapsed;
      this.amCaptain = amCaptain;
      NameLabel.Content = member.SummonerName;
      SummonerName = member.SummonerName;
      SummonerId = member.SummonerId;
      PlusPath.Visibility = member.HasInvitePower ? Visibility.Collapsed : Visibility.Visible;
    }

    private void UserControl_MouseEnter(object sender, MouseEventArgs e) {
      if (amCaptain && SummonerId != Client.LoginPacket.AllSummonerData.Summoner.SumId)
        KickButton.Visibility = GiveInviteButt.Visibility = Visibility.Visible;
    }

    private void UserControl_MouseLeave(object sender, MouseEventArgs e) {
      KickButton.Visibility = GiveInviteButt.Visibility = Visibility.Collapsed;
    }

    private void GotSummonerData(Task<PublicSummoner> task) {
      Dispatcher.Invoke(() => {
        ProfileIconImage.Source = LeagueData.GetProfileIconImage(LeagueData.GetIconData(task.Result.ProfileIconId));
        RiotServices.LeaguesService.GetAllLeaguesForPlayer(task.Result.SummonerId).ContinueWith(GotLeagueData);
        RankLabel.Content = "Level " + task.Result.SummonerLevel;
        NameLabel.Content = task.Result.Name;
      });
    }

    private void GotLeagueData(Task<SummonerLeaguesDTO> task) {
      var league = task.Result.SummonerLeagues.FirstOrDefault(l => l.Queue.Equals(QueueType.RANKED_SOLO_5x5.Key));
      if (league != null) {
        Dispatcher.Invoke(() => RankLabel.Content = RankedTier.Values[league.Tier] + " " + league.RequestorsRank);
      }
    }

    private void Kick_Click(object sender, RoutedEventArgs e) => KickClicked?.Invoke(this, new EventArgs());
    private void GiveInvite_Click(object sender, RoutedEventArgs e) => GiveInviteClicked?.Invoke(this, new EventArgs());
  }
}
