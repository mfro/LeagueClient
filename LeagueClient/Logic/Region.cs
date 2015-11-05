using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LeagueClient.Logic {
  public class Region {
    public string MainServer { get; internal set; }
    public string LoginQueueURL { get; internal set; }
    public string ChatServer { get; internal set; }

    public static readonly Region NA = new Region {
      MainServer = "prod.na2.lol.riotgames.com",
      LoginQueueURL = "https://lq.na2.lol.riotgames.com/",
      ChatServer = "chat.na2.lol.riotgames.com"
    };
    public static readonly Region PBE = new Region {
      MainServer = "prod.pbe1.lol.riotgames.com",
      LoginQueueURL = "https://lq.pbe1.lol.riotgames.com/",
      ChatServer = "chat.pbe1.lol.riotgames.com"
    };
  }
}
