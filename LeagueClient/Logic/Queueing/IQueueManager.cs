﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueClient.Logic.Cap;
using LeagueClient.Logic.Riot.Platform;

namespace LeagueClient.Logic.Queueing {
  public interface IQueueManager {
    void ShowQueuer(IQueuer queuer);
    void ShowQueuePopPopup(IQueuePopup popup);
    void JoinQueue(GameQueueConfig queue, string bots);
    void CreateLobby(GameQueueConfig queue, string bots);

    void CreateCapSolo();
    void CreateCapLobby();
    void EnterCapSolo(CapPlayer player);
    void JoinCapLobby(string groupId, int slotId);
  }
}