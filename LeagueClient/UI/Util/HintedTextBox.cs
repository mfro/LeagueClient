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
  public class HintedTextBox : TextBox {
    public string Hint {
      get { return (string) GetValue(HintProperty); }
      set { SetValue(HintProperty, value); }
    }

    public static readonly DependencyProperty HintProperty = DependencyProperty.Register("Hint",
      typeof(string), typeof(HintedTextBox), new PropertyMetadata(new PropertyChangedCallback(OnWatermarkChanged)));

    private bool isHinted = false;
    private Binding _textBinding = null;

    public HintedTextBox() {
      Loaded += (s, ea) => ShowWatermark();
    }

    protected override void OnGotFocus(RoutedEventArgs e) {
      base.OnGotFocus(e);
      HideWatermark();
    }

    protected override void OnLostFocus(RoutedEventArgs e) {
      base.OnLostFocus(e);
      ShowWatermark();
    }

    private static void OnWatermarkChanged(DependencyObject sender, DependencyPropertyChangedEventArgs ea) {
      var tbw = sender as HintedTextBox;
      if (tbw == null || !tbw.IsLoaded) return; //needed to check IsLoaded so that we didn't dive into the ShowWatermark() routine before initial Bindings had been made
      tbw.ShowWatermark();
    }

    private void ShowWatermark() {
      if (String.IsNullOrEmpty(Text) && !String.IsNullOrEmpty(Hint)) {
        isHinted = true;

        //save the existing binding so it can be restored
        _textBinding = BindingOperations.GetBinding(this, TextProperty);

        //blank out the existing binding so we can throw in our Watermark
        BindingOperations.ClearBinding(this, TextProperty);

        //set the signature watermark gray
        Foreground = new SolidColorBrush(Colors.Gray);

        //display our watermark text
        Text = Hint;
      }
    }

    private void HideWatermark() {
      if (isHinted) {
        isHinted = false;
        ClearValue(ForegroundProperty);
        Text = "";
        if (_textBinding != null) SetBinding(TextProperty, _textBinding);
      }
    }
  }
}
