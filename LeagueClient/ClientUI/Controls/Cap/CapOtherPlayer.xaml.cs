using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
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
using LeagueClient.Logic.Cap;

namespace LeagueClient.ClientUI.Controls {
  /// <summary>
  /// Interaction logic for CapOtherPlayer2.xaml
  /// </summary>
  public sealed partial class CapOtherPlayer : UserControl, IDisposable {
    public CapPlayer Player { get; private set; }

    private Timer timer;
    public bool editable;

    public CapOtherPlayer() {
      InitializeComponent();
    }

    public CapOtherPlayer(CapPlayer player, bool editable = false) : this() {
      Player = player;
      this.editable = editable;

      PositionBox.ItemsSource = Position.Values.Values.Where(p => p != Position.UNSELECTED);
      RoleBox.ItemsSource = Role.Values.Values.Where(p => p != Role.UNSELECTED);

      PositionText.Content = Player.Position?.Value;
      RoleText.Content = Player.Role?.Value;
      PositionBox.SelectedItem = Player.Position;
      RoleBox.SelectedItem = Player.Role;

      TimerText.Visibility = Visibility.Collapsed;

      if (Player.Champion != null)
        ChampionImage.Source = DataDragon.GetChampIconImage(Player.Champion).Load();
      else ChampionImage.Source = null;

      if (Player.Spell1 != null)
        Spell1Image.Source = DataDragon.GetSpellImage(Player.Spell1).Load();
      else Spell1Image.Source = null;

      if (Player.Spell2 != null)
        Spell2Image.Source = DataDragon.GetSpellImage(Player.Spell2).Load();
      else Spell2Image.Source = null;

      //Glow.Opacity = 0;
      if (editable && Player.Status == CapStatus.ChoosingAdvert) {
        PositionBox.Visibility = RoleBox.Visibility = Visibility.Visible;
        PositionText.Visibility = RoleText.Visibility = Visibility.Collapsed;
      } else {
        PositionBox.Visibility = RoleBox.Visibility = Visibility.Collapsed;
        PositionText.Visibility = RoleText.Visibility = Visibility.Visible;
      }

      if (editable && Player.Status == CapStatus.Found) {
        AcceptButton.Visibility = Visibility.Visible;
        DeclineButton.Visibility = Visibility.Visible;
      } else {
        AcceptButton.Visibility = Visibility.Collapsed;
        DeclineButton.Visibility = Visibility.Collapsed;
      }

      SummonerText.Visibility = Visibility.Visible;
      Unknown.Visibility = Visibility.Collapsed;
      TimerText.Visibility = Visibility.Collapsed;
      InnerBorder.Background = App.ForeBrush;
      switch (Player.Status) {
        case CapStatus.ChoosingAdvert:
          SummonerText.Content = Client.Strings.Cap.Player_Select_Advertising;
          InnerBorder.Background = App.Back1Brush;
          Unknown.Visibility = Visibility.Visible;
          SummonerText.Visibility = Visibility.Collapsed;
          break;
        case CapStatus.Searching:
          Unknown.Visibility = Visibility.Visible;
          Unknown.Text = "?";
          SummonerText.Content = Client.Strings.Cap.Player_Searching;
          break;
        case CapStatus.Found:
          SummonerText.Content = Client.Strings.Cap.Player_Found;
          TimerText.Visibility = Visibility.Visible;
          TimerText.Content = Math.Round(Player.TimeoutEnd.Subtract(DateTime.Now).TotalSeconds) + "";
          timer = new Timer { Interval = 1000, Enabled = true };
          timer.Elapsed += Timer_Elapsed;
          break;
        case CapStatus.Joining:
          SummonerText.Content = Client.Strings.Cap.Player_Joining;
          break;
        case CapStatus.Choosing:
        case CapStatus.Present:
          SummonerText.Content = Player.Name;
          break;
        case CapStatus.Ready:
          InnerBorder.Background = new SolidColorBrush(Color.FromRgb(0, 120, 54));
          goto case CapStatus.Present;
        case CapStatus.Penalty:
          SummonerText.Content = Client.Strings.Cap.Player_Kicked;
          Unknown.Visibility = Visibility.Visible;
          Unknown.Text = Math.Round(Player.TimeoutEnd.Subtract(DateTime.Now).TotalSeconds) + "";
          timer = new Timer { Interval = 1000, Enabled = true };
          timer.Elapsed += Timer_Elapsed;
          break;
        case CapStatus.SearchingDeclined:
          SummonerText.Content = Client.Strings.Cap.Player_Searching;
          break;
      }
    }

    private void Timer_Elapsed(object sender, ElapsedEventArgs e) {
      var t = (int) Math.Round(Player.TimeoutEnd.Subtract(DateTime.Now).TotalSeconds);
      if (t > 0) {
        try {
          Dispatcher.Invoke(() => TimerText.Content = Unknown.Text = t + "");
        } catch {
          Dispose();
        }
      } else {
        Dispose();
        Player.Status = CapStatus.Searching;
      }
    }

    private void AcceptButton_Click(object sender, RoutedEventArgs e) => Player.Accept(true);
    private void DeclineButton_Click(object sender, RoutedEventArgs e) => Player.Accept(false);
    private void KickButton_Click(object sender, RoutedEventArgs e) => Player.Kick();
    private void GiveInvite_Click(object sender, RoutedEventArgs e) => Player.GiveInvite();

    private void Champ_MouseEnter(object sender, MouseEventArgs e) {
      if (editable && Player.Status == CapStatus.Present) KickButton.BeginStoryboard(App.FadeIn);
      if (editable && Player.Status == CapStatus.Choosing) GiveInviteButt.BeginStoryboard(App.FadeIn);
    }

    private void Champ_MouseLeave(object sender, MouseEventArgs e) {
      KickButton.BeginStoryboard(App.FadeOut);
      GiveInviteButt.BeginStoryboard(App.FadeOut);
    }

    private void PositionBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
      if (Player.Position != PositionBox.SelectedItem)
        Player.ChangeProperty(nameof(Player.Position), (Position) PositionBox.SelectedItem);
    }

    private void RoleBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
      if (Player.Role != RoleBox.SelectedItem)
        Player.ChangeProperty(nameof(Player.Role), (Role) RoleBox.SelectedItem);
    }

    public void Dispose() {
      timer.Dispose();
    }
  }
}
