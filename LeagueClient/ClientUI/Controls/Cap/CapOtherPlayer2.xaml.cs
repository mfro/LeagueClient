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
using LeagueClient.Logic.Cap;

namespace LeagueClient.ClientUI.Controls {
  /// <summary>
  /// Interaction logic for CapOtherPlayer2.xaml
  /// </summary>
  public partial class CapOtherPlayer2 : UserControl {
    public CapPlayer Player { get; private set; }

    public event EventHandler<bool> CandidateReacted;
    public event EventHandler GiveInvite;
    public event EventHandler Kicked;

    private Timer timer;
    public bool editable;

    public CapOtherPlayer2() {
      InitializeComponent();
    }

    public CapOtherPlayer2(CapPlayer player, bool editable = false) {
      Player = player;
      this.editable = editable;

      InitializeComponent();
      PositionBox.ItemsSource = Position.Values.Values.Where(p => p != Position.UNSELECTED);
      RoleBox.ItemsSource = Role.Values.Values.Where(p => p != Role.UNSELECTED);

      Player.PropertyChanged += (s, e) => Dispatcher.Invoke(PlayerUpdate);
      Unknown.Visibility = Visibility.Collapsed;
      PlayerUpdate();
    }

    private void PlayerUpdate() {
      PositionText.Content = Player.Position?.Value;
      RoleText.Content = Player.Role?.Value;
      TimerText.Visibility = Visibility.Collapsed;

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

      Unknown.Visibility = Visibility.Collapsed;
      TimerText.Visibility = Visibility.Collapsed;
      switch (Player.Status) {
        case CapStatus.ChoosingAdvert:
          SummonerText.Content = "Select Position and Role";
          break;
        case CapStatus.Searching:
          Unknown.Visibility = Visibility.Visible;
          Unknown.Text = "?";
          SummonerText.Content = "Searching for candidate...";
          break;
        case CapStatus.Found:
          SummonerText.Content = "A candidate has been found";
          TimerText.Visibility = Visibility.Visible;
          TimerText.Content = Math.Round(Player.TimeoutEnd.Subtract(DateTime.Now).TotalSeconds) + "";
          timer = new Timer { Interval = 1000, Enabled = true };
          timer.Elapsed += Timer_Elapsed;
          break;
        case CapStatus.Joining:
          SummonerText.Content = "Waiting for candidate...";
          break;
        case CapStatus.Present:
          SummonerText.Content = Player.Name;
          break;
        case CapStatus.Ready:
          //Glow.Opacity = 1;
          goto case CapStatus.Present;
        case CapStatus.Penalty:
          SummonerText.Content = "Player kicked";
          Unknown.Visibility = Visibility.Visible;
          Unknown.Text = Math.Round(Player.TimeoutEnd.Subtract(DateTime.Now).TotalSeconds) + "";
          timer = new Timer { Interval = 1000, Enabled = true };
          timer.Elapsed += Timer_Elapsed;
          break;
        case CapStatus.SearchingDeclined:
          SummonerText.Content = "Searching for candidate...";
          break;
      }
    }

    private void Timer_Elapsed(object sender, ElapsedEventArgs e) {
      var t = (int) Math.Round(Player.TimeoutEnd.Subtract(DateTime.Now).TotalSeconds);
      if (t > 0) {
        try {
          Dispatcher.Invoke(() => TimerText.Content = Unknown.Text = t + "");
        } catch {
          timer.Dispose();
        }
      } else {
        timer.Dispose();
        Player.Status = CapStatus.Searching;
      }
    }

    private void AcceptButton_Click(object sender, RoutedEventArgs e) => CandidateReacted?.Invoke(this, true);
    private void DeclineButton_Click(object sender, RoutedEventArgs e) => CandidateReacted?.Invoke(this, false);
    private void KickButton_Click(object sender, RoutedEventArgs e) => Kicked?.Invoke(this, new EventArgs());
    private void GiveInvite_Click(object sender, RoutedEventArgs e) => GiveInvite?.Invoke(this, new EventArgs());

    private void Champ_MouseEnter(object sender, MouseEventArgs e) {
      if (editable && Player.Status == CapStatus.Present) KickButton.BeginStoryboard(App.FadeIn);
      if (editable && Player.Status == CapStatus.Choosing) GiveInviteButt.BeginStoryboard(App.FadeIn);
    }

    private void Champ_MouseLeave(object sender, MouseEventArgs e) {
      KickButton.BeginStoryboard(App.FadeOut);
      GiveInviteButt.BeginStoryboard(App.FadeOut);
    }
  }
}
