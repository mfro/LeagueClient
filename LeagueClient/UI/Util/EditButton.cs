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

namespace LeagueClient.UI.Util {
  /// <summary>
  /// Interaction logic for HintedTextBox.xaml
  /// </summary>
  public class EditButton : Button {
    //<Path x:Key="PencilPath" Data="M 0 15 L 2 10 11 1 14 4 5 13 Z M 3 9 L 6 12" Stroke="{StaticResource FontBrush}" StrokeLineJoin="Round" IsHitTestVisible="False"/>
    public EditButton() {
      Content = new Path {
        Data = Geometry.Parse("M 0 15 L 2 10 11 1 14 4 5 13 Z M 3 9 L 6 12"),
        Stroke = (Brush) FindResource("FontBrush"),
        StrokeLineJoin = PenLineJoin.Round,
        IsHitTestVisible = false,
      };
    }
  }
}
