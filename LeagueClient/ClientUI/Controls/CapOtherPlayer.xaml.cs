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
using LeagueClient.Logic.Cap;
using LeagueClient.Logic.Riot.Platform;
using MFroehlich.League.Assets;
using MFroehlich.Parsing.DynamicJSON;
using static LeagueClient.Logic.Strings;

namespace LeagueClient.ClientUI.Controls {
  /// <summary>
  /// Interaction logic for TeambuilderPlayer.xaml
  /// </summary>
  public partial class CapOtherPlayer : UserControl {
    public CapPlayer Player { get; private set; }

    public CapOtherPlayer(CapPlayer player) {
      Player = player;
      InitializeComponent();
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
      switch (Player.Status) {
        case CapStatus.Ready:
          Check.Visibility = Visibility.Visible;
          goto case CapStatus.Present;
        case CapStatus.Present:
          SummonerText.Text = Player.Name;
          break;
        case CapStatus.Penalty:
          SummonerText.Text = "Player kicked.";
          break;
        case CapStatus.Searching:
          SummonerText.Text = "Searching for candidate...";
          break;
        case CapStatus.SearchingDeclined:
          SummonerText.Text = "The player was not found, searching for another candidate...";
          break;
        case CapStatus.Found:
          SummonerText.Text = "A candidate has been found.";
          break;
        case CapStatus.Choosing:
          break;//TODO
      }
    }
  }
}
