using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LeagueClient.Logic {
  public class Strings {
    public static Dictionary<string, Strings> Localizations = new Dictionary<string, Strings> {
      ["en_US"] = new Strings {
        Cap = new CapStrings {
          Player_Select_Advertising = "Select position and role",
          Player_Searching = "Searching",
          Player_Found = "Candidate found",
          Player_Joining = "Candidate joining",
          Player_Kicked = "Player Kicked",
          //TODO Strings
        },
        Landing = new LandingStrings {
          Logout_Button = "Logout",
          Home_Button = "Home",
          Profile_Button = "Profile",
          Shop_Button = "Shop",
        }
      }
    };

    public CapStrings Cap { get; set; }
    public LandingStrings Landing { get; set; }
  }

  public class CapStrings {
    public string Player_Select_Advertising { get; set; }
    public string Player_Searching { get; set; }
    public string Player_Found { get; set; }
    public object Player_Joining { get; set; }
    public object Player_Kicked { get; set; }
  }

  public class LandingStrings {
    public string Home_Button { get; set; }
    public string Logout_Button { get; set; }
    public string Profile_Button { get; set; }
    public string Shop_Button { get; set; }
  }
}
