using LeagueClient.Logic;
using LeagueClient.Logic.Riot.Platform;
using MFroehlich.League.Assets;
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

namespace LeagueClient.ClientUI.Controls {
  /// <summary>
  /// Interaction logic for TBDPlayer.xaml
  /// </summary>
  public partial class TBDPlayer : UserControl {
    public event EventHandler KickClicked;
    public event EventHandler GiveInviteClicked;
    public event EventHandler<RoleChangedEventArgs> RoleSelected;
    public bool CanControl { get; set; }

    public long SummonerId { get; }
    public string SummonerName { get; private set; }

    private bool isSelf;

    public TBDPlayer() {
      InitializeComponent();
    }

    public TBDPlayer(bool isSelf, bool amCaptain, Member member, int profileIconId) : this() {
      KickButton.Visibility = GiveInviteButt.Visibility = Visibility.Collapsed;
      CanControl = !isSelf && amCaptain;

      NameLabel.Content = member.SummonerName;
      SummonerName = member.SummonerName;
      SummonerId = member.SummonerId;

      ProfileIconImage.Source = DataDragon.GetProfileIconImage(DataDragon.GetIconData(profileIconId)).Load();
      PlusPath.Visibility = member.HasInvitePower ? Visibility.Collapsed : Visibility.Visible;
      Client.Session.SummonerCache.GetData(member.SummonerName, GotSummoner);

      PrimaryCombo.Visibility = SecondaryCombo.Visibility = isSelf ? Visibility.Visible : Visibility.Collapsed;

      PrimaryCombo.ItemsSource = TBDRole.Values;
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
        NameLabel.Content = SummonerName = item.Data.Summoner.Name;
      });
    }

    private void PrimaryCombo_SelectionChanged(object sender, SelectionChangedEventArgs e) => 
      RoleSelected?.Invoke(this, new RoleChangedEventArgs(0, PrimaryCombo.SelectedItem as TBDRole));

    private void SecondaryCombo_SelectionChanged(object sender, SelectionChangedEventArgs e) =>
      RoleSelected?.Invoke(this, new RoleChangedEventArgs(1, SecondaryCombo.SelectedItem as TBDRole));

    private void Kick_Click(object sender, RoutedEventArgs e) => KickClicked?.Invoke(this, new EventArgs());
    private void GiveInvite_Click(object sender, RoutedEventArgs e) => GiveInviteClicked?.Invoke(this, new EventArgs());
  }

  public class RoleChangedEventArgs {
    public int RoleIndex { get; }
    public TBDRole Role { get; }
    public RoleChangedEventArgs(int index, TBDRole role) {
      RoleIndex = index;
      Role = role;
    }
  }
}
