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
using LeagueClient.RiotInterface.Chat;
using LeagueClient.RiotInterface.Riot;
using LeagueClient.RiotInterface.Riot.Platform;

namespace LeagueClient.ClientUI {
  /// <summary>
  /// Interaction logic for TeambuilderLobbyPage.xaml
  /// </summary>
  public partial class TeambuilderLobbyPage : Page {
    public LobbyStatus Status { get; private set; }

    public TeambuilderLobbyPage() {
      InitializeComponent();
      Client.ChatManager.UpdateStatus(LeagueStatus.InTeamBuilder, StatusShow.Chat);

      Client.MessageReceived += MessageReceived;
      RiotCalls.CreateGroupFinderLobby(61, "abc125-abc125-abc125-abc125").ContinueWith(GotLobbyStatus);
      //TODO Add control for me in teambuilder and add it to this
    }

    private void GotLobbyStatus(Task<LobbyStatus> task) {
      if (!task.IsFaulted) Status = task.Result;
      Console.WriteLine(task.Result.GameData);
    } 

    private void MessageReceived(object sender, RtmpSharp.Messaging.MessageReceivedEventArgs e) {
      if(e.Body is LobbyStatus) {
        Status = (LobbyStatus) e.Body;
      }
      Console.WriteLine(e.Body);
    }

    private void Invite_Click(object sender, RoutedEventArgs e) {
      RiotCalls.Invite(53026814);
    }
  }
}
