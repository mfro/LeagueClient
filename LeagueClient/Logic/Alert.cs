using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using LeagueClient.UI.Client.Alerts;
using RiotClient.Riot.Platform;

namespace LeagueClient.Logic {
  public interface Alert {
    event EventHandler<bool> Close;

    UIElement Popup { get; }
    UIElement History { get; }
  }

  public static class AlertFactory {
    private static T Create<T>(params object[] args) where T : Alert {
      var types = new Type[args.Length];
      for (int i = 0; i < args.Length; i++) types[i] = args[i]?.GetType();
      return (T) Application.Current.Dispatcher.MyInvoke(typeof(T).GetConstructor(types).Invoke, args);
    }

    public static OkAlert KickedFromCap() => Create<OkAlert>("Kicked from Group", "You have been kicked from a teambuilder group and returned to the queue.");
    public static OkAlert QueueDodger() => Create<OkAlert>("Unabled to join queue", "Someone in your group has cancelled too many queues recently and is temporarily unabled to queue for games");
    public static OkAlert GroupDisbanded() => Create<OkAlert>("Your group has disbanded", "The group you were in was disbanded for an unknown reason");

    public static GameInviteAlert InviteAlert(InvitationRequest invite) => Create<GameInviteAlert>(invite);
  }
}
