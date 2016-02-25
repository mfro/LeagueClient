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

namespace LeagueClient.UI.Login {
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
        if (value == LoginAccountState.Loading) {
          LoadingBorder.BeginStoryboard((Storyboard) LoadingBorder.FindResource("LoaderStory"));
        } else LoadingBorder.Margin = new Thickness(-5, -5, 123, 118);
        state = value;
      }
    }

    private LoginAccountState state;

    public LoginAccount() {
      InitializeComponent();
    }

    public LoginAccount(string login, string name, int profileicon) : this() {
      try {
        ProfileIcon.Source = DataDragon.GetProfileIconImage(DataDragon.GetIconData(profileicon)).Load();
      } catch { }
      NameLabel.Content = name;
      Username = login;
      State = LoginAccountState.Normal;
    }

    private void MenuItem_Click(object sender, RoutedEventArgs e) {
      Remove?.Invoke(this, e);
    }

    #region Mouse Animation Handling
    private void Grid_MouseEnter(object sender, MouseEventArgs e) {

      NameBorder.BeginStoryboard(App.FadeIn);
    }

    private void Grid_MouseLeave(object sender, MouseEventArgs e) {
      if (State == LoginAccountState.Loading) return;

      NameBorder.BeginStoryboard(App.FadeOut);
    }

    private void Grid_MouseDown(object sender, MouseButtonEventArgs e) {
      if (e.ChangedButton != MouseButton.Left || State != LoginAccountState.Normal) return;

      var anim = new ThicknessAnimation(new Thickness(8, 8, 8, 5), ButtonDuration);
      MainBorder.BeginAnimation(BorderThicknessProperty, anim);
    }

    private void Grid_MouseUp(object sender, MouseButtonEventArgs e) {
      if (e.ChangedButton != MouseButton.Left || state != LoginAccountState.Normal) return;

      Click?.Invoke(this, e);

      var anim = new ThicknessAnimation(new Thickness(5), ButtonDuration);
      MainBorder.BeginAnimation(BorderThicknessProperty, anim);
    }
    #endregion
  }

  public enum LoginAccountState {
    Normal, Readonly, Loading
  }
}
