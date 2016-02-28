using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueClient.UI.Client;
using RiotClient.Lobbies;

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

    void JoinLobby(Lobby lobby);

    void ShowPage();
    /// <summary>
    /// Shows a generic alert notification in the alerts area
    /// </summary>
    /// <param name="alert">The alert to show</param>
    void ShowNotification(Alert alert);

    void ShowInfo(IQueueInfo info);

    void ViewProfile(string summonerName);
  }
}
