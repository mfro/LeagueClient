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
using LeagueClient.ClientUI.Controls;
using LeagueClient.Logic.Chat;
using LeagueClient.Logic.Riot;
using LeagueClient.Logic.Riot.Platform;
using MFroehlich.League.DataDragon;
using MFroehlich.Parsing.DynamicJSON;

namespace LeagueClient.ClientUI {
  /// <summary>
  /// Interaction logic for TeambuilderLobbyPage.xaml
  /// </summary>
  public partial class CapLobbyPage : Page {
    public LobbyStatus Status { get; private set; }
    public JSONObject GroupData { get; private set; }

    public CapLobbyPage() {
      InitializeComponent();
      Client.ChatManager.UpdateStatus(LeagueStatus.InTeamBuilder, StatusShow.Chat);

      Client.MessageReceived += MessageReceived;
      //TODO Add control for me in teambuilder and add it to this
    }

    public CapLobbyPage(int slotId) {
      InitializeComponent();
      Client.ChatManager.UpdateStatus(LeagueStatus.InTeamBuilder, StatusShow.Chat);

      Client.MessageReceived += MessageReceived;
    }

    public void GotLobbyStatus(LobbyStatus status) {
      Status = status;
    }

    private async void MessageReceived(object sender, RtmpSharp.Messaging.MessageReceivedEventArgs e) {
      var status = e.Body as LobbyStatus;
      var response = e.Body as LcdsServiceProxyResponse;
      if (status != null) {
        GotLobbyStatus(status);
      } else if (response != null) {
        if (response.status.Equals("OK")) {
          switch (response.methodName) {
            case "groupCreatedV3":
              GroupData = JSON.ParseObject(response.payload);
              var data = await RiotCalls.GameInvitationService.CreateGroupFinderLobby(61, GroupData["groupId"]);
              GotLobbyStatus(data);
              break;
            default:
              Client.Log("Unhandled response to {0}, {1}", response.methodName, response.payload);
              break;
          }
        } else if (!response.status.Equals("ACK")) Client.TryBreak(response.status);
      } else { }
    }
   }

  public class TeambuilderPlayer {
    public ChampionDto Champ { get; set; }
    public SpellDto Spell1 { get; set; }
    public SpellDto Spell2 { get; set; }
    public string Position { get; set; }
    public string Role { get; set; }
  }
}