using LeagueClient.Logic;
using MFroehlich.League.Assets;
using RiotClient;
using RiotClient.Lobbies;
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

namespace LeagueClient.UI.Client.Lobbies {
  /// <summary>
  /// Interaction logic for TBDPlayer.xaml
  /// </summary>
  public partial class TBDPlayer : UserControl {
    public event EventHandler KickClicked;
    public event EventHandler GiveInviteClicked;
    public event EventHandler<RoleChangedEventArgs> RoleSelected;
    public bool CanControl { get; set; }

    private TBDLobbyMember slot;

    public TBDPlayer() {
      InitializeComponent();
    }

    public TBDPlayer(bool amCaptain, TBDLobbyMember slot, int profileIconId) : this() {
      KickButton.Visibility = GiveInviteButt.Visibility = Visibility.Collapsed;
      CanControl = !slot.IsMe && amCaptain;

      this.slot = slot;
      slot.Changed += Slot_Changed;

      NameLabel.Content = slot.Name;

      ProfileIconImage.Source = DataDragon.GetProfileIconImage(DataDragon.GetIconData(profileIconId)).Load();
      PlusPath.Visibility = slot.HasInvitePower ? Visibility.Collapsed : Visibility.Visible;
      Session.Current.SummonerCache.GetData(slot.Name, GotSummoner);

      PrimaryCombo.Visibility = SecondaryCombo.Visibility = slot.IsMe ? Visibility.Visible : Visibility.Collapsed;

      PrimaryCombo.ItemsSource = TBDRole.Values.Values;
      SecondaryCombo.ItemsSource = TBDRole.Values.Values;
      SecondaryCombo.IsEnabled = false;
    }

    private void Slot_Changed(object sender, EventArgs e) {
      PrimaryLabel.Content = slot.PrimaryRole;
      SecondaryLabel.Content = slot.SecondaryRole;
      SecondaryCombo.IsEnabled = slot.PrimaryRole != TBDRole.UNSELECTED && slot.PrimaryRole != TBDRole.FILL;
      SecondaryLabel.Visibility = slot.PrimaryRole == TBDRole.FILL ? Visibility.Collapsed : Visibility.Visible;

      PrimaryCombo.ItemsSource = TBDRole.Values.Values.Where(role => role != slot.SecondaryRole && role != TBDRole.UNSELECTED);
      SecondaryCombo.ItemsSource = TBDRole.Values.Values.Where(role => role != slot.PrimaryRole && role != TBDRole.UNSELECTED);
    }

    private void UserControl_MouseEnter(object sender, MouseEventArgs e) {
      if (CanControl && slot.SummonerID != Session.Current.Account.SummonerID)
        KickButton.Visibility = GiveInviteButt.Visibility = Visibility.Visible;
    }

    private void UserControl_MouseLeave(object sender, MouseEventArgs e) {
      KickButton.Visibility = GiveInviteButt.Visibility = Visibility.Collapsed;
    }

    private void GotSummoner(SummonerCache.Item item) {
      Dispatcher.Invoke(() => {
        ProfileIconImage.Source = DataDragon.GetProfileIconImage(DataDragon.GetIconData(item.Data.Summoner.ProfileIconId)).Load();
        NameLabel.Content = item.Data.Summoner.Name;
      });
    }

    private void PrimaryCombo_SelectionChanged(object sender, SelectionChangedEventArgs e) {
      if (PrimaryCombo.SelectedItem == null) return;
      RoleSelected?.Invoke(this, new RoleChangedEventArgs(0, PrimaryCombo.SelectedItem as TBDRole));
      PrimaryCombo.SelectedItem = null;
    }

    private void SecondaryCombo_SelectionChanged(object sender, SelectionChangedEventArgs e) {
      if (SecondaryCombo.SelectedItem == null) return;
      RoleSelected?.Invoke(this, new RoleChangedEventArgs(1, SecondaryCombo.SelectedItem as TBDRole));
      SecondaryCombo.SelectedItem = null;
    }

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
