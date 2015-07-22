using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using MFroehlich.Parsing;

namespace LeagueClient {
  /// <summary>
  /// Interaction logic for App.xaml
  /// </summary>
  public partial class App : Application {
    [Mine]
    public static Storyboard
      ButtonHover, ButtonUnHover, ButtonPress, ButtonRelease,
      FadeIn, FadeOut, ComboItemEnter, ComboItemLeave,
      UnreadPulse;

    [Mine]
    public static SolidColorBrush
      FontColor, HighColor, ForeColor, Back1Color, Back2Color,
      BusyColor, AwayColor, ChatColor;

    [Mine]
    public static Style Control;

    public void LoadResources() {
      foreach (var field in typeof(App).GetFields())
        if (Attribute.IsDefined(field, typeof(MineAttribute)))
          field.SetValue(this, FindResource(field.Name));
    }

    public void ButtonMouseLeave(object src, EventArgs args) {
      var butt = (Button) src;
      var release = ButtonRelease;
      try {
        release.GetCurrentState(butt);
        release.Remove(butt);
      } catch { }
    }

    private void Application_Exit(object sender, ExitEventArgs e) {
      Client.Settings.Save();
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

  public class MineAttribute : Attribute { }
}
