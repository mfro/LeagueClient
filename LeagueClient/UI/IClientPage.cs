using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RtmpSharp.Messaging;

namespace LeagueClient.UI {
  interface IClientPage {

    bool HandleMessage(MessageReceivedEventArgs args);
  }
}
