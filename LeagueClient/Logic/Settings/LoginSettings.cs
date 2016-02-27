using RiotClient.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LeagueClient.Logic.Settings {
  public class LoginSettings : ISettings {
    public List<string> Accounts { get; }  = new List<string>();
  }
}
