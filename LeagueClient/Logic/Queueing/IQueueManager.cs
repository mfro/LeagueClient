using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueClient.ClientUI.Controls;
using LeagueClient.ClientUI.Main;
using LeagueClient.Logic.Cap;
using LeagueClient.Logic.Riot.Platform;

namespace LeagueClient.Logic.Queueing {
  public interface IQueueManager {
    /// <summary>
    /// Shows a queue manager UI in the status panel area
    /// </summary>
    /// <param name="queuer">The queue manager</param>
    void ShowQueuePopup(IQueuePopup queuer);
    /// <summary>
    /// Shows a page UI in the main area
    /// </summary>
    /// <param name="page"></param>
    void ShowPage(IClientSubPage page);

    void ShowPage();
    /// <summary>
    /// Shows a generic alert notification in the alerts area
    /// </summary>
    /// <param name="alert">The alert to show</param>
    void ShowNotification(Alert alert);
    /// <summary>
    /// Begins champion select with the game
    /// </summary>
    /// <param name="game"></param>
    void BeginChampionSelect(GameDTO game);

    bool AttachToQueue(SearchingForMatchNotification parms);

    void AcceptInvite(InvitationRequest invite);

    /// <summary>
    /// Creates a teambuilder solo query selection and displays it
    /// </summary>
    //void CreateCapSolo();

    /// <summary>
    /// Creates a teambuilder lobby and displays it
    /// Note: This invokes the Riot API to create the lobby
    /// </summary>
    //void CreateCapLobby();

    /// <summary>
    /// Displays a new teambuilder lobby with a preset solo queuery, usually because of solo queueing
    /// Note: This does not invoke the Riot API
    /// </summary>
    /// <param name="groupId">The ID of the teambuilder group</param>
    /// <param name="slotId">The slot to join</param>
    /// <param name="player">The teambuilder information of the solo query</param>
    //void JoinCapLobby(CapPlayer player);
    /// <summary>
    /// Displays a new teambuilder lobby, usually because of direct invite
    /// Note: This does not invoke the Riot API
    /// </summary>
    //void JoinCapLobby(Task<LobbyStatus> pending);
  }
}
