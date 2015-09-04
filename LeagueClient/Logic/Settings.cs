using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using MFroehlich.Parsing;
using MFroehlich.Parsing.DynamicJSON;

namespace LeagueClient.Logic {
  [JSONSerializable]
  public class Settings {
    public string Username { get; set; }
    public string Password { get; set; }
    public int ProfileIcon { get; set; }
    public string SummonerName { get; set; }
  }
}
