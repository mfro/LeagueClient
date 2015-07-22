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
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace LeagueClient.ClientUI.Controls {
  /// <summary>
  /// Interaction logic for MyProgressBar.xaml
  /// </summary>
  public partial class MyProgressBar : UserControl {

    public double Value {
      get { return value; }
      set {
        if (!IsIndeterminate)
          AnimateTo(ActualWidth * value);
        this.value = value;
      }
    }
    public bool IsIndeterminate {
      get { return indeterminate; }
      set {
        indeterminate = value;
        if (value) {
          var animate = new DoubleAnimation {
            From = -IndeterminateBarWidth,
            To = ActualWidth,
            Duration = new Duration(TimeSpan.FromSeconds(IndeterminateAnimationTime)),
            RepeatBehavior = RepeatBehavior.Forever
          };
          Bar.BeginAnimation(UserControl.WidthProperty, null);
          Bar.Width = IndeterminateBarWidth;
          Bar.RenderTransform = new TranslateTransform();
          Bar.RenderTransform.BeginAnimation(TranslateTransform.XProperty, animate);
        } else {
          Bar.RenderTransform = null;
          Bar.Width = ActualWidth * this.value;
          Value = this.value;
        }
      }
    }

    public int IndeterminateBarWidth { get; set; }
    public double IndeterminateAnimationTime { get; set; }

    private bool indeterminate;
    private double value;

    public MyProgressBar() {
      SizeChanged += (src, e) => IsIndeterminate = indeterminate;
      IndeterminateBarWidth = 40;
      IndeterminateAnimationTime = 1.5;
      Foreground = App.HighColor;
      InitializeComponent();
    }

    private void AnimateTo(double width) {
      var anim = new DoubleAnimation(width, new Duration(TimeSpan.FromSeconds(.1)));
      Bar.BeginAnimation(UserControl.WidthProperty, anim);
    }
  }
}
