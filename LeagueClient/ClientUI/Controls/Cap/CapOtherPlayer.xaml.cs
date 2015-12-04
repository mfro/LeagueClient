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
using LeagueClient.Logic.Riot.Platform;
using MFroehlich.League.Assets;
using MFroehlich.Parsing.DynamicJSON;

namespace LeagueClient.ClientUI.Controls {
  /// <summary>
  /// Interaction logic for TeambuilderPlayer.xaml
  /// </summary>
  public partial class CapOtherPlayer : UserControl {
    public CapPlayer Player { get; private set; }

    public event EventHandler<bool> CandidateReacted;

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

      Player.PropertyChanged += (s, e) => Dispatcher.Invoke(PlayerUpdate);
      Unknown.Visibility = Visibility.Collapsed;
      PlayerUpdate();
    }

    private void PlayerUpdate() {
      if (Player.Position != null) {
        RoleText.Text = Player.Position.Value;
        if (Player.Role != null) RoleText.Text += " / " + Player.Role.Value;
      } else if (Player.Role != null) {
        RoleText.Text = Player.Role.Value;
      } else RoleText.Text = "";

      Check.Visibility = Visibility.Collapsed;
      if(editable && Player.Status == CapStatus.ChoosingAdvert) {
        PositionBox.Visibility = Visibility.Visible;
        RoleBox.Visibility = Visibility.Visible;
      } else {
        PositionBox.Visibility = Visibility.Collapsed;
        RoleBox.Visibility = Visibility.Collapsed;
      }

      if(editable && Player.Status == CapStatus.Found) {
        AcceptButt.Visibility = Visibility.Visible;
        DeclineButt.Visibility = Visibility.Visible;
      } else {
        AcceptButt.Visibility = Visibility.Collapsed;
        DeclineButt.Visibility = Visibility.Collapsed;
      }

      TimeoutText.Visibility = Visibility.Collapsed;
      switch (Player.Status) {
        case CapStatus.ChoosingAdvert:
          SummonerText.Text = "Select Position and Role";
          break;
        case CapStatus.Searching:
          Unknown.Visibility = Visibility.Visible;
          SummonerText.Text = "Searching for candidate...";
          break;
        case CapStatus.Found:
          SummonerText.Text = "A candidate has been found";
          TimeoutText.Visibility = Visibility.Visible;
          TimeoutText.Text = Math.Round(Player.TimeoutEnd.Subtract(DateTime.Now).TotalSeconds) + "";
          timer = new Timer { Interval = 1000, Enabled = true };
          timer.Elapsed += Timer_Elapsed;
          break;
        case CapStatus.Joining:
          SummonerText.Text = "Waiting for candidate to join group...";
          break;
        case CapStatus.Present:
          SummonerText.Text = Player.Name;
          break;
        case CapStatus.Ready:
          Check.Visibility = Visibility.Visible;
          goto case CapStatus.Present;
        case CapStatus.Penalty:
          SummonerText.Text = "Player kicked";
          TimeoutText.Visibility = Visibility.Visible;
          TimeoutText.Text = Math.Round(Player.TimeoutEnd.Subtract(DateTime.Now).TotalSeconds) + "";
          timer = new Timer { Interval = 1000, Enabled = true };
          timer.Elapsed += Timer_Elapsed;
          break;
        case CapStatus.SearchingDeclined:
          SummonerText.Text = "The player was not found, searching for another candidate...";
          break;
      }
    }

    private void Timer_Elapsed(object sender, ElapsedEventArgs e) {
      var t = (int) Math.Round(Player.TimeoutEnd.Subtract(DateTime.Now).TotalSeconds);
      if(t > 0) {
        Dispatcher.Invoke(() => TimeoutText.Text = t + "");
      } else {
        timer.Dispose();
        Player.Status = CapStatus.Searching;
      }
    }

    private void Accept_Click(object sender, RoutedEventArgs e) {
      CandidateReacted?.Invoke(this, true);
    }

    private void Decline_Click(object sender, RoutedEventArgs e) {
      CandidateReacted?.Invoke(this, false);
    }
  }
}
