using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LeagueClient {
  public class Alert {
    public event EventHandler<bool> Reacted;

    public string Title { get; set; }
    public string Message { get; set; }
    public AlertType Type { get; set; }

    public Alert(string title, string message, AlertType type) {
      Title = title;
      Message = message;
      Type = type;
    }

    public void ReactYesNo(bool yesno) {
      Reacted?.Invoke(this, yesno);
    }

    public enum Priority {
      Normal, Warning, Error
    }

    public enum AlertType {
      YesNo, Ok
    }

    public static Alert KickedFromCap() {
      return new Alert("Kicked from Group",
        "You have been kicked from a teambuilder group and returned to the queue.",
        AlertType.Ok);
    }

    public static Alert Teambuilder(string user, EventHandler<bool> OnReact) {
      var a = new Alert("Teambuilder Invite",
        user + " has invited you to a teambuilder game",
        AlertType.YesNo);
      a.Reacted += OnReact;
      return a;
    }
  }
}
