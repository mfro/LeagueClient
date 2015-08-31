using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using LeagueClient.Logic.Chat;

namespace LeagueClient.ClientUI.Controls {
  /// <summary>
  /// Interaction logic for InvitePopup.xaml
  /// </summary>
  public partial class InvitePopup : UserControl {
    public Dictionary<string, bool> Users { get; } = new Dictionary<string, bool>();
    private List<BuddyInfo> old = new List<BuddyInfo>();

    public event EventHandler Close;

    public InvitePopup() {
      InitializeComponent();
      if (Client.Connected) {
        Client.ChatManager.ChatListUpdated += (s, e) => Dispatcher.MyInvoke(FriendList_ListChanged, e);
      }
    }

    private void FriendList_ListChanged(IEnumerable<Friend> user) {
      var list = new List<BuddyInfo>();
      foreach(var item in user) {
        if (!Users.ContainsKey(item.User.JID.User)) Users[item.User.JID.User] = false;
        var info = new BuddyInfo(item.UserName, item.User.JID.User, item.SummonerIcon);
        if (Users[info.User]) info.Visibility = Visibility.Visible;
        list.Add(info);
      }
      bool same = true;
      if (list.Count != old.Count) same = false;
      else
        for (int i = 0; i < list.Count; i++)
          if (!list[i].Equals(old[i])) same = false;
      if (!same) {
        ItemsList.ItemsSource = list;
        old = list;
      }
    }

    private void Close_Click(object sender, RoutedEventArgs e) {
      Close?.Invoke(this, new EventArgs());
    }

    private void Grid_MouseUp(object sender, MouseButtonEventArgs e) {
      var item = (sender as UserControl).DataContext as BuddyInfo;
      Users[item.User] = !Users[item.User];
      item.Visibility = Users[item.User] ? Visibility.Visible : Visibility.Collapsed;
    }
  }

  public class BuddyInfo : INotifyPropertyChanged {
    public event PropertyChangedEventHandler PropertyChanged;

    public string Name {
      get { return name; }
      set { SetField(ref name, value); }
    }
    public BitmapImage SummonerIcon {
      get { return summonerIcon; }
      set { SetField(ref summonerIcon, value); }
    }
    public string User {
      get { return user; }
      set { SetField(ref user, value); }
    }
    public Visibility Visibility {
      get { return visibility; }
      set { SetField(ref visibility, value); }
    }

    private string name;
    private BitmapImage summonerIcon;
    private string user;
    private Visibility visibility;

    public BuddyInfo(string name, string user, BitmapImage summonerIcon) {
      Name = name;
      User = user;
      Visibility = Visibility.Collapsed;
      SummonerIcon = summonerIcon;
    }

    private void SetField<T>(ref T field, T value, [System.Runtime.CompilerServices.CallerMemberName] string name = null) {
      if (!(field?.Equals(value) ?? false)) {
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
      }
    }

    public override bool Equals(object obj) {
      var other = obj as BuddyInfo;
      return other != null && User.Equals(other.User);
    }

    public override int GetHashCode() {
      return user.GetHashCode();
    }
  }
}
