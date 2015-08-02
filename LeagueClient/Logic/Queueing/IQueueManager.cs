using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueClient.ClientUI.Controls;
using LeagueClient.Logic.Cap;
using LeagueClient.Logic.Riot.Platform;

namespace LeagueClient.Logic.Queueing {
  public interface IQueueManager {
    /// <summary>
    /// Shows a queue manager UI in the status panel area
    /// </summary>
    /// <param name="queuer">The queue manager</param>
    void ShowQueuer(IQueuer queuer);
    ///// <summary>
    ///// Enters a standard queue
    ///// </summary>
    ///// <param name="queue">The queue to join</param>
    ///// <param name="bots">Optional bot difficulty paramter</param>
    //void JoinQueue(GameQueueConfig queue, string bots = null);
    /// <summary>
    /// Creates a standard queue lobby
    /// </summary>
    /// <param name="queue">The queue to create a lobby for</param>
    /// <param name="bots">Optional bot difficulty paramter</param>
    void CreateLobby(GameQueueConfig queue, string bots = null);

    /// <summary>
    /// Shows a generic alert notification in the alerts area
    /// </summary>
    /// <param name="alert">The alert to show</param>
    void ShowNotification(Alert alert);

    /// <summary>
    /// Creates a teambuilder solo query selection and displays it
    /// </summary>
    void CreateCapSolo();

    /// <summary>
    /// Creates a teambuilder lobby and displays it
    /// </summary>
    void CreateCapLobby();

    //void EnterCapSolo(CapMePlayer player);
    //void ReEnterCapSolo(CapMePlayer player);

    /// <summary>
    /// Joines a teambuilder lobby and displays it
    /// </summary>
    /// <param name="groupId">The ID of the teambuilder group</param>
    /// <param name="slotId">The slot to join</param>
    /// <param name="player">The teambuilder information of the solo query</param>
    void JoinCapLobby(string groupId, int slotId, CapPlayer player);
  }
}
