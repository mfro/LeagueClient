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

namespace LeagueClient.ClientUI.Controls {
  /// <summary>
  /// Interaction logic for InvitePopup.xaml
  /// </summary>
  public partial class InvitePopup : UserControl {
    public InvitePopup() {
      InitializeComponent();
      var list = new List<object>();
      foreach(var friend in Client.ChatManager.FriendList) {
        list.Add(new { SummonerIcon = friend.SummonerIcon, Name = friend.UserName });
      }
      ItemsList.ItemsSource = list;
      //TODO Finish invitepopup
    }

    private void Close_Click(object sender, RoutedEventArgs e) {

    }
  }
}
