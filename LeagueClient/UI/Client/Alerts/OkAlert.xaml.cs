using System;
using System.Collections.Generic;
using System.ComponentModel;
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

namespace LeagueClient.UI.Client.Alerts {
  /// <summary>
  /// Interaction logic for OkAlert.xaml
  /// </summary>
  public partial class OkAlert : UserControl, Alert, INotifyPropertyChanged {
    public event EventHandler<bool> Close;
    public event PropertyChangedEventHandler PropertyChanged;

    public string Message {
      get { return message; }
      set { SetField(ref message, value); }
    }
    public string Title {
      get { return title; }
      set { SetField(ref title, value); }
    }

    private string message;
    private string title;

    public OkAlert() {
      InitializeComponent();
    }

    public OkAlert(string title, string message) : this() {
      Title = title;
      Message = message;

      HistoryGrid.DataContext = this;
      PopupGrid.DataContext = this;
      MainGrid.Children.Remove(HistoryGrid);
      MainGrid.Children.Remove(PopupGrid);
    }

    public UIElement Popup => PopupGrid;
    public UIElement History => HistoryGrid;

    private void Ok_Click(object sender, RoutedEventArgs e) {
      Close?.Invoke(this, true);
    }

    private void Close_Click(object sender, RoutedEventArgs e) {
      Close?.Invoke(this, false);
    }
    private void CloseAgain_Click(object sender, RoutedEventArgs e) => Close?.Invoke(this, true);

    private void SetField<T>(ref T field, T value, [System.Runtime.CompilerServices.CallerMemberName] string name = null) {
      if (!(field?.Equals(value) ?? false)) {
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
      }
    }
  }
}
