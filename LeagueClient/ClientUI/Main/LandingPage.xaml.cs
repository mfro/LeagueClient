using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using LeagueClient.ClientUI.Controls;
using LeagueClient.Logic;
using LeagueClient.Logic.Chat;
using LeagueClient.Logic.Queueing;
using RtmpSharp.Messaging;

namespace LeagueClient.ClientUI.Main {
  /// <summary>
  /// Interaction logic for LandingPage.xaml
  /// </summary>
  public partial class LandingPage : Page, IClientSubPage {
    public LandingPage() {
      InitializeComponent();

      Client.ChatManager.FriendList.ListChanged += FriendList_ListChanged;
    }

    private void FriendList_ListChanged(object sender, System.ComponentModel.ListChangedEventArgs e) {
      var groups = new Dictionary<object, List<ChatFriend>> {
        ["Chat"] = new List<ChatFriend>(),
        ["Away"] = new List<ChatFriend>(),
        ["Dnd"] = new List<ChatFriend>()
      };
      foreach (var item in Client.ChatManager.FriendList) {
        if (item.CurrentGameInfo != null) {
          if (!groups.ContainsKey(item.CurrentGameInfo.gameId))
            groups[item.CurrentGameInfo.gameId] = new List<ChatFriend>();
          groups[item.CurrentGameInfo.gameId].Add(item);
        }
      }
      foreach (var item in new List<object>(groups.Keys)) {
        if (groups[item].Count == 1) groups.Remove(item);
      }

      foreach (var item in Client.ChatManager.FriendList) {
        if (groups.Any(pair => pair.Value.Contains(item))) continue;
        groups[item.Status.Show.ToString()].Add(item);
      }
      Dispatcher.Invoke(() => {
        GroupList.Children.Clear();
        foreach (var group in groups.Where(pair => pair.Value.Count > 0).OrderBy(pair => pair.Value.First().GetValue())) {
          GroupList.Children.Add(new ItemsControl { ItemsSource = group.Value.OrderBy(u => u.GetValue()) });
        }
      });
    }

    private void Friend_MouseUp(object sender, MouseButtonEventArgs e) {
      if (e.ChangedButton == MouseButton.Left)
        Client.ChatManager.AddChat(((FriendListItem2) sender).friend);
    }

    public bool CanClose => false;
    public bool CanPlay => true;
    public Page Page => this;
    public event EventHandler Close;
    public void ForceClose() {
      throw new NotImplementedException();
    }

    public IQueuer HandleClose() {
      throw new NotImplementedException();
    }

    public bool HandleMessage(MessageReceivedEventArgs args) => false;
  }
}
