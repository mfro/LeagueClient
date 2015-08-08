using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using LeagueClient.Logic.Riot;
using MFroehlich.Parsing;

namespace LeagueClient {
  /// <summary>
  /// Interaction logic for App.xaml
  /// </summary>
  public partial class App : Application {
    [Resource]
    public static Storyboard
      //ButtonHover, ButtonUnHover, ButtonPress, ButtonRelease,
      FadeIn, FadeOut, ComboItemEnter, ComboItemLeave;

    [Resource]
    public static SolidColorBrush
      FontBrush, FocusBrush, ForeBrush, Back1Brush, Back2Brush,
      BusyBrush, AwayBrush, ChatBrush;

    [Resource]
    public static Style Control;

    public void LoadResources() {
      foreach (var field in typeof(App).GetFields())
        if (Attribute.IsDefined(field, typeof(ResourceAttribute)))
          field.SetValue(this, FindResource(field.Name));
    }

    public void ButtonMouseLeave(object src, EventArgs args) {
      //var butt = (Button) src;
      //var release = ButtonRelease;
      //try {
      //  release.GetCurrentState(butt);
      //  release.Remove(butt);
      //} catch { }
    }

    public static void Focus(UIElement el) {
      App.Current.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Input, new Action(() => {
        el.Focus();
        System.Windows.Input.Keyboard.Focus(el);
      }));
    }

    private void Application_Exit(object sender, ExitEventArgs e) {
      Client.Settings.Save();
      RiotCalls.LoginService.Logout();
    }
  }

  public static class Extensions {
    public static T WaitForResult<T>(this Task<T> t) {
      t.Wait();
      return t.Result;
    }

    public static void WriteString(this Stream stm, string str) {
      stm.Write(Encoding.UTF8.GetBytes(str));
    }
  }

  public class ResourceAttribute : Attribute { }
}
