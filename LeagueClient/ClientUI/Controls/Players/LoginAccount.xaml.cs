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
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MFroehlich.League.Assets;

namespace LeagueClient.ClientUI.Controls {
  /// <summary>
  /// Interaction logic for LoginAccount.xaml
  /// </summary>
  public partial class LoginAccount : UserControl {
    private static readonly Duration
      MoveDuration = new Duration(TimeSpan.FromMilliseconds(200)),
      ButtonDuration = new Duration(TimeSpan.FromMilliseconds(80));

    private static readonly AnimationTimeline
      ExpandSlow = new ThicknessAnimation(new Thickness(5), MoveDuration),
      ContractSlow = new ThicknessAnimation(new Thickness(0), MoveDuration),
      ExpandFast = new ThicknessAnimation(new Thickness(5), ButtonDuration),
      ContractFast = new ThicknessAnimation(new Thickness(0), ButtonDuration);

    public event EventHandler Click;
    public event EventHandler Remove;

    public string Username { get; private set; }
    public LoginAccountState State {
      get { return state; }
      set {
        Loader.Visibility = value == LoginAccountState.Loading ? Visibility.Visible : Visibility.Collapsed;
        state = value;
      }
    }

    private LoginAccountState state;

    public LoginAccount() {
      InitializeComponent();
    }

    public LoginAccount(string login, string name, int profileicon) : this() {
      ProfileIcon.Source = LeagueData.GetProfileIconImage(LeagueData.GetIconData(profileicon));
      NameLabel.Content = name;
      Username = login;
      State = LoginAccountState.Normal;
    }

    private void MenuItem_Click(object sender, RoutedEventArgs e) {
      Remove?.Invoke(this, e);
    }

    #region Mouse Animation Handling
    private void Grid_MouseEnter(object sender, MouseEventArgs e) {
      if (State != LoginAccountState.Normal) return;

      MainBorder.BeginAnimation(MarginProperty, ContractSlow);
      MainBorder.BeginAnimation(BorderThicknessProperty, ExpandSlow);
    }

    private void Grid_MouseLeave(object sender, MouseEventArgs e) {
      MainBorder.BeginAnimation(MarginProperty, ExpandSlow);
      MainBorder.BeginAnimation(BorderThicknessProperty, ContractSlow);
    }

    private void Grid_MouseDown(object sender, MouseButtonEventArgs e) {
      if (e.ChangedButton != MouseButton.Left || State != LoginAccountState.Normal) return;

      MainBorder.BeginAnimation(MarginProperty, ExpandFast);
    }

    private void Grid_MouseUp(object sender, MouseButtonEventArgs e) {
      if (e.ChangedButton != MouseButton.Left) return;

      Click?.Invoke(this, e);

      //MainBorder.BeginAnimation(MarginProperty, ContractFast);
      MainBorder.BeginAnimation(BorderThicknessProperty, ContractFast);
    }
    #endregion
  }

  public enum LoginAccountState {
    Normal, Readonly, Loading
  }
}
