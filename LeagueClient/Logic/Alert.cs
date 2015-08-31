using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using LeagueClient.ClientUI.Controls;

namespace LeagueClient.Logic {
  public interface Alert {
    event EventHandler<AlertEventArgs> Handled;

    string Title { get; set; }
    string Message { get; set; }
    UIElement Control { get; }
  }

  public static class AlertFactory {
    private static Alert Create<T>(params object[] args) where T : Alert {
      var types = new Type[args.Length];
      for (int i = 0; i < args.Length; i++) types[i] = args[i]?.GetType();
      T t = (T) App.Current.Dispatcher.MyInvoke(typeof(T).GetConstructor(types).Invoke, args);
      return t;
    }

    public static Alert KickedFromCap() => Create<OkAlert>("Kicked from Group", "You have been kicked from a teambuilder group and returned to the queue.");
    public static Alert QueueDodger() => Create<OkAlert>("Unabled to join queue", "You have cancelled too many queues recently and are temporarily unabled to queue for games");
    public static Alert GroupDisbanded() => Create<OkAlert>("Your group has disbanded", "The group you were in was disbanded for an unknown reason");

    public static Alert TeambuilderInvite(string user) => Create<YesNoAlert>("Teambuilder Invite", "You have been invited to a teambuilder game lobby by " + user);
    public static Alert CustomInvite(string user) => Create<YesNoAlert>("Custom Game Invite", "You have been invited to a custom game lobby by " + user);
  }

  public class AlertEventArgs : EventArgs {
    public object Data { get; private set; }

    public AlertEventArgs(object data) {
      Data = data;
    }
  }
}
