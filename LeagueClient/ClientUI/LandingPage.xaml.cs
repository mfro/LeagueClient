using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using LeagueClient.ClientUI.Controls;
using LeagueClient.ClientUI.Main;
using LeagueClient.Logic;
using LeagueClient.Logic.Chat;
using LeagueClient.Logic.Queueing;
using LeagueClient.Logic.Riot.Platform;
using RtmpSharp.Messaging;

namespace LeagueClient.ClientUI {
  /// <summary>
  /// Interaction logic for LandingPage.xaml
  /// </summary>
  public partial class LandingPage : Page, IClientPage, IQueueManager {
    public IClientSubPage CurrentPage { get; private set; }

    private Border currentButton;

    private List<Border> Buttons;
    private int[] ArrowLocations = new[] { 11, 53, 95, 134, 172 };

    public LandingPage() {
      InitializeComponent();
      Buttons = new List<Border> { LogoutTab, PlayTab, FriendsTab, ProfileTab, ShopTab };

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

    private void Tab_MouseEnter(object sender, RoutedEventArgs e) {
      var text = ((Border) sender).Child as TextBlock;
      text.Foreground = Brushes.White;
    }

    private void Tab_MouseLeave(object sender, RoutedEventArgs e) {
      if (sender != currentButton) {
        var text = ((Border) sender).Child as TextBlock;
        text.Foreground = App.FontBrush;
      }
    }

    private void Tab_MousePress(object sender, RoutedEventArgs e) {
      ShowPage(Buttons.IndexOf((Border) sender));
    }

    private void ShowPage(int index) {
      if (currentButton != null) ((TextBlock) currentButton.Child).Foreground = App.FontBrush;
      currentButton = Buttons[index];

      if (index == 0) Client.Logout();

      var arrowAnim = new ThicknessAnimation(new Thickness(15, ArrowLocations[index], 15, 0), new Duration(TimeSpan.FromMilliseconds(100)));
      Arrows.BeginAnimation(MarginProperty, arrowAnim);

      var slideAnim = new ThicknessAnimation(new Thickness(0, -680 * (index - 1), 0, 0), new Duration(TimeSpan.FromMilliseconds(100)));
      SlidingGrid.BeginAnimation(MarginProperty, slideAnim);

      if (index == 1) PlayPage.Reset();
    }

    private void Grid_MouseDown(object sender, MouseButtonEventArgs e) {
      if (e.GetPosition((Grid) sender).Y < 20) {
        if (e.ClickCount == 2) Client.MainWindow.Center();
        else Client.MainWindow.DragMove();
      }
    }

    private void Animate(Button butt, string key) => butt.BeginStoryboard((Storyboard) butt.FindResource(key));

    public bool HandleMessage(MessageReceivedEventArgs args) {
      if (CurrentPage?.HandleMessage(args) ?? false) return true;
      return false;
    }

    public void ShowQueuer(IQueuer queuer) {
      //throw new NotImplementedException();
    }

    public void ShowPage(IClientSubPage page) {
      if (Thread.CurrentThread != Dispatcher.Thread) { Dispatcher.MyInvoke(ShowPage, page); return; }

      CloseSubPage(true);
      page.Close += HandlePageClose;
      CurrentPage = page;
      SubPageArea.Content = page?.Page;
      SubPageArea.Visibility = Visibility.Visible;
    }

    public void ShowNotification(Alert alert) {
      //throw new NotImplementedException();
    }

    public void BeginChampionSelect(GameDTO game) {
      //throw new NotImplementedException();
    }

    private void CloseSubPage(bool notifyPage) {
      if (Thread.CurrentThread != Dispatcher.Thread) { Dispatcher.MyInvoke(CloseSubPage, notifyPage); return; }

      if (CurrentPage != null) {
        CurrentPage.Close -= HandlePageClose;
        if (notifyPage) {
          var x = CurrentPage.HandleClose();
          if (x != null) ShowQueuer(x);
        }
        SubPageArea.Content = null;
        CurrentPage = null;
        SubPageArea.Visibility = Visibility.Collapsed;
      }
    }

    private void HandlePageClose(object source, EventArgs e) {
      if (Thread.CurrentThread != Dispatcher.Thread)
        Dispatcher.MyInvoke(CloseSubPage, false);
      else
        CloseSubPage(false);
    }
  }
}
