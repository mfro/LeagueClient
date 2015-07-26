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
using LeagueClient.Logic;
using LeagueClient.Logic.Riot.Platform;

namespace LeagueClient.ClientUI.Controls {
  /// <summary>
  /// Interaction logic for TeambuilderSoloQueuePopup.xaml
  /// </summary>
  public partial class TeambuilderSoloQueuePopup : UserControl, IQueuePopup {
    public event EventHandler Accepted;
    public event EventHandler Cancelled;

    public TeambuilderSoloQueuePopup() {
      InitializeComponent();
    }

    private void Accept_Click(object sender, RoutedEventArgs e) {
      if (Accepted != null) Accepted(this, new EventArgs());
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) {
      if (Cancelled != null) Cancelled(this, new EventArgs());
    }

    public Control GetControl() {
      return this;
    }

    public GameQueueConfig GetQueue() {
      throw new NotImplementedException();
    }
  }
}
