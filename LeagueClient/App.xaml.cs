using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using LeagueClient.Logic;
using MFroehlich.Parsing;
using System.Windows.Input;
using RiotClient;

namespace LeagueClient {
  /// <summary>
  /// Interaction logic for App.xaml
  /// </summary>
  public partial class App : Application {
    #region Resources
    [Resource]
    public static Storyboard
      //ButtonHover, ButtonUnHover, ButtonPress, ButtonRelease,
      FadeIn, FadeOut;//, ComboItemEnter, ComboItemLeave;

    [Resource]
    public static Color
      FontColor, FocusColor, ForeColor, Back1Color, Back2Color,
      BusyColor, AwayColor, ChatColor;

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
    #endregion

    private void Application_Exit(object sender, ExitEventArgs e) {
      if (Session.Current?.Connected == true) {
        Session.Current.Logout();
      }
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

    public static bool CanAssignTo<T>(this Type type) {
      return typeof(T).IsAssignableFrom(type);
    }

    public static string RemoveAllWhitespace(this string str) {
      return string.Join("", str.Split(default(string[]), StringSplitOptions.RemoveEmptyEntries));
    }

    public static string Substring(this string str, string prefix, string suffix) {
      int start = str.IndexOf(prefix) + prefix.Length;
      if (start < prefix.Length) throw new ArgumentException($"Prefix {prefix} not found in string {str}");
      int end = str.IndexOf(suffix, start);
      if (end < 0) throw new ArgumentException($"Suffix {suffix} not found in string {str} after {prefix}");
      return str.Substring(start, end - start);
    }

    public static void MyFocus(this UIElement el) {
      Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.SystemIdle, (Action) (() => {
        if (!el.Focus()) el.MyFocus();
      }));
    }

    public static void MyInvoke<T1>(this Dispatcher dispatch, Action<T1> func, T1 t) => dispatch.Invoke(func, t);
    public static void MyInvoke<T1, T2>(this Dispatcher dispatch, Action<T1, T2> func, T1 t, T2 t1) => dispatch.Invoke(func, t, t1);
    public static void MyInvoke<T1, T2, T3>(this Dispatcher dispatch, Action<T1, T2, T3> func, T1 t, T2 t1, T3 t2) => dispatch.Invoke(func, t, t1, t2);
    public static void MyInvoke<T1, T2, T3, T4>(this Dispatcher dispatch, Action<T1, T2, T3, T4> func, T1 t, T2 t1, T3 t2, T4 t3) => dispatch.Invoke(func, t, t1, t2, t3);
    public static R MyInvoke<T1, R>(this Dispatcher dispatch, Func<T1, R> func, T1 t) => (R) dispatch.Invoke(func, t);
    public static R MyInvoke<T1, T2, R>(this Dispatcher dispatch, Func<T1, T2, R> func, T1 t, T2 t1) => (R) dispatch.Invoke(func, t, t1);
    public static R MyInvoke<T1, T2, T3, R>(this Dispatcher dispatch, Func<T1, T2, T3, R> func, T1 t, T2 t1, T3 t2) => (R) dispatch.Invoke(func, t, t1, t2);
    public static R MyInvoke<T1, T2, T3, T4, R>(this Dispatcher dispatch, Func<T1, T2, T3, T4, R> func, T1 t, T2 t1, T3 t2, T4 t3) => (R) dispatch.Invoke(func, t, t1, t2, t3);
  }

  public class ResourceAttribute : Attribute { }

  public class PointD {
    public double X { get; set; }
    public double Y { get; set; }

    public PointD(double x, double y) {
      X = x; Y = y;
    }
  }
}
