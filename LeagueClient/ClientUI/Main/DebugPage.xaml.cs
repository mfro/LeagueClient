using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
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
using LeagueClient.Logic.Queueing;
using LeagueClient.Logic.Riot;
using LeagueClient.Logic.Riot.Platform;
using MFroehlich.Parsing.DynamicJSON;

namespace LeagueClient.ClientUI.Main {
  /// <summary>
  /// Interaction logic for DebugPage.xaml
  /// </summary>
  public partial class DebugPage : Page, IClientSubPage {
    public BindingList<Type> Services { get; } = new BindingList<Type>();
    public BindingList<MethodInfo> Methods { get; } = new BindingList<MethodInfo>();
    public List<string> IgnoredMethods { get; } = new List<string>();
    private Dictionary<VarInfo, TextBox> parameters = new Dictionary<VarInfo, TextBox>();

    public DebugPage() {
      InitializeComponent();
      Client.MessageReceived += Client_MessageReceived;
      foreach (var method in typeof(object).GetMethods())
        IgnoredMethods.Add(method.Name);

      foreach (var service in typeof(RiotCalls).GetNestedTypes().Where(t => t.Name.EndsWith("Service")))
        Services.Add(service);
      ServiceList.SelectionChanged += Service_Selected;
      MethodList.SelectionChanged += Method_Selected;
    }

    private void Client_MessageReceived(object sender, MessageHandlerArgs e) {
      e.Handled = true;
      LcdsServiceProxyResponse lcds;
      if ((lcds = e.InnerEvent.Body as LcdsServiceProxyResponse) != null) {
        Dispatcher.Invoke(() => Messages.Text += $"[{lcds.status}] {lcds.methodName}: {lcds.payload}\n");
      } else {
        Dispatcher.Invoke(() => Messages.Text += e.InnerEvent.Body.GetType().Name+'\n');
      }
    }

    private void Method_Invoke(object sender, RoutedEventArgs e) {
      var method = MethodList.SelectedItem as MethodInfo;
      if (method == null) return;
      var parms = method.GetParameters();
      var args = new object[parms.Length];
      JSON.RequreStructureAttribute = false;

      for(int i = 0; i < parms.Length; i++) {
        var info = new VarInfo(parms[i].Name, parms[i].ParameterType);
        var text = parameters[info].Text;

        try {
          if (typeof(String).IsAssignableFrom(parms[i].ParameterType)) {
            args[i] = text;
          } else if (parms[i].ParameterType.IsValueType) {
            args[i] = Convert.ChangeType(text, parms[i].ParameterType);
          } else if (parms[i].ParameterType.IsArray) {
            args[i] = JSON.CastTo(JSON.ParseArray(text), parms[i].ParameterType);
          } else {
            args[i] = JSON.CastTo(JSON.ParseObject(text), parms[i].ParameterType);
          }
        } catch { Client.Log("Failed to parse parameter {0}, content: {1}", parms[i].Name, parms[i].ParameterType); }
      }

      JSON.RequreStructureAttribute = true;

      method.Invoke(null, args);
    }

    private void Method_Selected(object sender, SelectionChangedEventArgs e) {
      ParamList.Children.Clear();
      InvokeButton.Visibility = Visibility.Collapsed;
      if (MethodList.SelectedIndex < 0) return;
      var method = MethodList.SelectedItem as MethodInfo;
      MethodName.Text = method.Name;

      parameters.Clear();
      foreach(var param in method.GetParameters()){
        var grid = CreateParameter(new VarInfo(param.Name, param.ParameterType));
        ParamList.Children.Add(grid);
      }
      InvokeButton.Visibility = Visibility.Visible;
    }

    private void Service_Selected(object sender, SelectionChangedEventArgs e) {
      Methods.Clear();
      ParamList.Children.Clear();
      InvokeButton.Visibility = Visibility.Collapsed;
      if (ServiceList.SelectedIndex < 0) return;
      foreach(var method in ((Type) ServiceList.SelectedItem).GetMethods().Where(m => !IgnoredMethods.Contains(m.Name)))
        Methods.Add(method);
    }

    private Grid CreateParameter(VarInfo info) {
      var grid = new Grid { Background = App.Back1Brush, Margin = new Thickness(0, 0, 0, 10) };
      grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
      grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
      grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
      var name = new TextBlock { Style = App.Control, FontSize = 18, Margin = new Thickness(4, 1, 4, 2) };
      var type = new TextBlock { Style = App.Control, FontSize = 18, Margin = new Thickness(4, 1, 4, 2) };

      if (!info.Type.IsValueType) {
        type.MouseUp += (s, e) => {
          var window = new Window { Width = 300, Background = App.Back2Brush, Title = info.Type.Name };
          window.SizeToContent = SizeToContent.Height;
          window.Topmost = true;
          window.Deactivated += (s2, e2) => window.Topmost = true;
          var stack = new StackPanel();
          foreach(var prop in info.Type.GetProperties()) {
            var grid2 = new Grid();
            var name2 = new TextBlock { Style = App.Control, FontSize = 16, Margin = new Thickness(4, 1, 4, 2) };
            var type2 = new TextBlock { Style = App.Control, FontSize = 16, Margin = new Thickness(4, 1, 4, 2) };
            name2.Text = prop.Name;
            type2.Text = prop.PropertyType.Name;
            type2.HorizontalAlignment = HorizontalAlignment.Right;
            grid2.Children.Add(name2);
            grid2.Children.Add(type2);
            stack.Children.Add(grid2);
          }
          window.Content = stack;
          window.Show();
        };
      }

      var input = new TextBox { FontSize = 16, Padding = new Thickness(4, 1, 4, 2), TextWrapping = TextWrapping.Wrap, AcceptsReturn = true };
      parameters[info] = input;
      grid.Children.Add(input);
      Grid.SetRow(input, 1);

      type.HorizontalAlignment = HorizontalAlignment.Right;
      name.Text = info.Name;
      type.Text = info.Type.Name;

      grid.Children.Add(name);
      grid.Children.Add(type);
      return grid;
    }
    public event EventHandler Close;

    public bool CanPlay() => false;
    public Page GetPage() => this;

    public void ForceClose() { }
    public IQueuer HandleClose() {
      Client.MessageReceived -= Client_MessageReceived;
      return null;
    }

    /*
<Grid Background="{StaticResource Back1Brush}">
 <Grid.RowDefinitions>
   <RowDefinition Height="auto"/>
   <RowDefinition Height="auto"/>
   <RowDefinition Height="auto"/>
 </Grid.RowDefinitions>
 <TextBlock Style="{StaticResource Control}" FontSize="18" Margin="4 1 4 2">Username</TextBlock>
 <TextBlock Style="{StaticResource Control}" FontSize="18" Margin="4 1 24 2" HorizontalAlignment="Right">string</TextBlock>
 <!--<TextBox Grid.Row="1" FontSize="16" Padding="4 1 4 2" TextWrapping="Wrap" AcceptsReturn="True">Hello, my name is harold</TextBox>-->
 <Grid Grid.Row="1" Height="20">
   <Grid.ColumnDefinitions>
     <ColumnDefinition Width="22"/>
     <ColumnDefinition Width="*"/>
   </Grid.ColumnDefinitions>
   <Path Stroke="{StaticResource FontBrush}" StrokeThickness="2" Data="M 10 2 L 10 18" Grid.ColumnSpan="2" Name="Plus"/>
   <Path Stroke="{StaticResource FontBrush}" StrokeThickness="2" Data="M 2 10 L 18 10" Grid.ColumnSpan="2"/>
   <TextBlock Style="{StaticResource Control}" Grid.Column="1" FontSize="14">Expand Parameter</TextBlock>
 </Grid>
 <StackPanel Grid.Row="2" Margin="10 0 0 0"/>
</Grid>
*/

    private class VarInfo {
      public string Name { get; private set; }
      public Type Type { get; private set; }

      public VarInfo(string name, Type type) {
        Name = name;
        Type = type;
      }

      public override bool Equals(object obj) {
        var other = obj as VarInfo;
        return other != null && Name.Equals(other.Name) && Type.Equals(other.Type);
      }

      public override int GetHashCode() {
        return Name.GetHashCode() + Type.GetHashCode();
      }
    }
  }
}
