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
    public bool CanControl { get; set; }

    public long SummonerId;
    public string SummonerName;

    public LobbyPlayer2() {
      InitializeComponent();
    }

    public LobbyPlayer2(bool amCaptain, Member member, int profileIconId) : this() {
      KickButton.Visibility = GiveInviteButt.Visibility = Visibility.Collapsed;
      CanControl = amCaptain;
      NameLabel.Content = member.SummonerName;
      SummonerName = member.SummonerName;
      SummonerId = member.SummonerId;
      ProfileIconImage.Source = DataDragon.GetProfileIconImage(DataDragon.GetIconData(profileIconId)).Load();
      PlusPath.Visibility = member.HasInvitePower ? Visibility.Collapsed : Visibility.Visible;
      Client.Session.SummonerCache.GetData(member.SummonerName, GotSummoner);
    }

    private void UserControl_MouseEnter(object sender, MouseEventArgs e) {
      if (CanControl && SummonerId != Client.Session.LoginPacket.AllSummonerData.Summoner.SummonerId)
        KickButton.Visibility = GiveInviteButt.Visibility = Visibility.Visible;
    }

    private void UserControl_MouseLeave(object sender, MouseEventArgs e) {
      KickButton.Visibility = GiveInviteButt.Visibility = Visibility.Collapsed;
    }

    private void GotSummoner(SummonerCache.Item item) {
      Dispatcher.Invoke(() => {
        ProfileIconImage.Source = DataDragon.GetProfileIconImage(DataDragon.GetIconData(item.Data.Summoner.ProfileIconId)).Load();
        RankLabel.Content = "Level " + item.Data.SummonerLevel.Level;
        NameLabel.Content = item.Data.Summoner.Name;
        var league = item.Leagues.SummonerLeagues.FirstOrDefault(l => l.Queue.Equals(QueueType.RANKED_SOLO_5x5.Key));
        if (league != null) RankLabel.Content = RankedTier.Values[league.Tier] + " " + league.Rank;
      });
    }

    private void Kick_Click(object sender, RoutedEventArgs e) => KickClicked?.Invoke(this, new EventArgs());
    private void GiveInvite_Click(object sender, RoutedEventArgs e) => GiveInviteClicked?.Invoke(this, new EventArgs());
  }
}
