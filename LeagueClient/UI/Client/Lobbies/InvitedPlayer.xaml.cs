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
  /// Interaction logic for InvitedPlayer.xaml
  /// </summary>
  public partial class InvitedPlayer : UserControl {
    private LobbyInvitee player;

    public InvitedPlayer(LobbyInvitee player) {
      InitializeComponent();

      this.player = player;
      this.player.Changed += (s, e) => Dispatcher.Invoke(Update);
      Update();
    }

    private void Update() {
      NameText.Content = player.Name;
      switch (player.State) {
        case "QUIT": StateText.Content = "Quit"; break;
        case "PENDING": StateText.Content = "Pending"; break;
        case "ACCEPTED": StateText.Content = "Accepted"; break;
        case "DECLINED": StateText.Content = "Declined"; break;
        default: break;
      }
    }
  }
}
