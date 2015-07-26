using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LeagueClient {
  public class Alert {
    public string Title { get; set; }
    public string Message { get; set; }
    public AlertType Type { get; set; }
    private Action<bool> Action { get; set; }

    public Alert(string title, string message, AlertType type) {
      Title = title;
      Message = message;
      Type = type;
    }

    public void ReactYesNo(bool yesno) {
      Action(yesno);
    }

    public enum Priority {
      Normal, Warning, Error
    }

    public enum AlertType {
      YesNo, Ok
    }

    public static Alert Teambuilder(string user, Action<bool> OnReact) {
      return new Alert("Teambuilder Invite",
        user + " has invited you to a teambuilder game",
        AlertType.YesNo) { Action = OnReact };
    }
  }
}
