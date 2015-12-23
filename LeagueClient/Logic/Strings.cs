using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LeagueClient.Logic {
  public class Strings {

    public static Strings en_US { get; } = new Strings {
      Cap = new CapStrings {
        Player_Select_Advertising = "Select position and role",
        Player_Searching = "Searching",
        Player_Found = "Candidate found",
        Player_Joining = "Candidate joining",
        Player_Kicked = "Player Kicked",
        //TODO Strings
      }
    };

    public CapStrings Cap { get; set; }
  }

  public class CapStrings {
    public string Player_Select_Advertising { get; set; }
    public string Player_Searching { get; set; }
    public string Player_Found { get; set; }
    public object Player_Joining { get; set; }
    public object Player_Kicked { get; set; }
  }
}
