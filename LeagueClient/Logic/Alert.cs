using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LeagueClient.Logic {
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

    public static Alert KickedFromCap { get; } = new Alert("Kicked from Group",
      "You have been kicked from a teambuilder group and returned to the queue.", AlertType.Ok);

    public static Alert QueueDodger { get; } = new Alert("Unabled to join queue",
      "You have cancelled too many queues recently and are temporarily unabled to queue for games", AlertType.Ok);

    public static Alert GroupDisbanded { get; } = new Alert("Your group has disbanded",
      "The group you were in was disbanded for an unknown reason", AlertType.Ok);

    public static Alert TeambuilderInvite(string user, EventHandler<bool> OnReact) {
      var a = new Alert("Teambuilder Invite",
        user + " has invited you to a teambuilder game",
        AlertType.YesNo);
      a.Reacted += OnReact;
      return a;
    }

  }
}
