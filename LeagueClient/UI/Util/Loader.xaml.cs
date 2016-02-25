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

namespace LeagueClient.UI.Util {
  /// <summary>
  /// Interaction logic for Loader.xaml
  /// </summary>
  public partial class Loader : UserControl {
    private const int AnimationTime = 500;

    public Loader() {
      SizeChanged += Loader_SizeChanged;
      InitializeComponent();
      Start();
    }

    private async void Start() {
      double margin = ActualHeight / 30;
      Section1.Margin = Section2.Margin = Section3.Margin = new Thickness(margin);

      var anim = new DoubleAnimation(ActualHeight / 2, ActualHeight - margin * 2, new Duration(TimeSpan.FromMilliseconds(AnimationTime)));
      anim.RepeatBehavior = RepeatBehavior.Forever;
      anim.AutoReverse = true;
      anim.AccelerationRatio = .85;

      Section1.Height = Section2.Height = Section3.Height = anim.From.Value;
      Section1.BeginAnimation(HeightProperty, anim);
      await Task.Delay(AnimationTime / 3);
      Section2.BeginAnimation(HeightProperty, anim);
      await Task.Delay(AnimationTime / 3);
      Section3.BeginAnimation(HeightProperty, anim);
    }

    private void Loader_SizeChanged(object sender, SizeChangedEventArgs e) {
      Start();
    }
  }
}
