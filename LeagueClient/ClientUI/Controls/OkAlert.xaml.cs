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

namespace LeagueClient.ClientUI.Controls {
  /// <summary>
  /// Interaction logic for OkAlert.xaml
  /// </summary>
  public partial class OkAlert : UserControl, Alert, INotifyPropertyChanged {
    public OkAlert(string title, string message) {
      InitializeComponent();
      Title = title;
      Message = message;
    }

    public event EventHandler<AlertEventArgs> Handled;
    public event PropertyChangedEventHandler PropertyChanged;

    public UIElement Control => this;
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

    private void Ok_Click(object sender, RoutedEventArgs e) {
      Handled?.Invoke(this, new AlertEventArgs(null));
    }

    private void SetField<T>(ref T field, T value, [System.Runtime.CompilerServices.CallerMemberName] string name = null) {
      if (!(field?.Equals(value) ?? false)) {
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
      }
    }
  }
}
