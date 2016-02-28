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
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using LeagueClient.Logic;
using LeagueClient.Logic.Queueing;
using RtmpSharp.Messaging;
using RiotClient.Riot.Platform;
using RiotClient;
using RiotClient.Lobbies;

namespace LeagueClient.UI.Client.Custom {
  /// <summary>
  /// Interaction logic for CustomCreatePage.xaml
  /// </summary>
  public sealed partial class CustomCreatePage : Page, IClientSubPage {
    public event EventHandler Close;

    private static readonly Duration
      MoveDuration = new Duration(TimeSpan.FromMilliseconds(200)),
      ButtonDuration = new Duration(TimeSpan.FromMilliseconds(80));

    private static readonly AnimationTimeline
      MarginShrink = new ThicknessAnimation(new Thickness(MarginSize - BorderSize, MarginSize - BorderSize, -3, -3), MoveDuration),
      BorderExpand = new ThicknessAnimation(new Thickness(BorderSize), MoveDuration),
      MarginExpand = new ThicknessAnimation(new Thickness(MarginSize, MarginSize, 0, 0), MoveDuration),
      BorderShrink = new ThicknessAnimation(new Thickness(0), MoveDuration);

    private const int BorderSize = 3;
    private const int MarginSize = 10;
    private GameMap selected;
    private Border current;

    public CustomCreatePage() {
      InitializeComponent();
      SummonersRift.Tag = GameMap.SummonersRift;
      CrystalScar.Tag = GameMap.TheCrystalScar;
      TwistedTreeline.Tag = GameMap.TheTwistedTreeline;
      HowlingAbyss.Tag = GameMap.HowlingAbyss;
      foreach (var border in new[] { SummonersRift, CrystalScar, TwistedTreeline, HowlingAbyss }) {
        border.MouseEnter += Border_MouseEnter;
        border.MouseLeave += Border_MouseLeave;
        border.MouseUp += Border_MouseUp;
      }
      current = SummonersRift;
      Spectators.ItemsSource = SpectatorState.Values.Values;
      Spectators.SelectedItem = SpectatorState.ALL;
      GameType.ItemsSource = new[] { GameConfig.Blind, GameConfig.Draft, GameConfig.AllRandom };
      GameType.SelectedItem = GameConfig.Blind;
      GameName.Text = Session.Current.Account.Name + "'s Game";
      GameName.CaretIndex = GameName.Text.Length;
      Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Input, (Func<bool>) GameName.Focus);

      MapSelected();
    }

    private void MapSelected() {
      selected = (GameMap) current.Tag;
      current.Effect = new DropShadowEffect { BlurRadius = 15, Color = App.FocusColor, ShadowDepth = 0 };
      MapLabel.Content = selected.DisplayName;
      int old = (int) (TeamSize.SelectedItem ?? 0);
      var size = new int[selected.TotalPlayers / 2];
      for (int i = 0; i < size.Length; i++) size[i] = size.Length - i;
      TeamSize.ItemsSource = size;
      TeamSize.SelectedIndex = 0;
    }

    private async void CreateGame(object sender, RoutedEventArgs e) {
      ErrorLabel.Visibility = Visibility.Collapsed;
      var config = new PracticeGameConfig();
      config.GameName = GameName.Text;
      config.GamePassword = GamePass.Text;
      config.MaxNumPlayers = ((int) TeamSize.SelectedItem) * 2;
      config.GameTypeConfig = (GameType.SelectedItem as GameConfig).Key;
      config.AllowSpectators = (Spectators.SelectedItem as SpectatorState).Key;
      config.GameMap = selected;
      switch (selected.MapId) {
        case 8: config.GameMode = "ODIN"; break;
        case 10:
        case 11: config.GameMode = "CLASSIC"; break;
        case 12:
        case 14: config.GameMode = "ARAM"; break;
      }

      try {
        var lobby = await CustomLobby.CreateLobby(config);
        LoLClient.QueueManager.JoinLobby(lobby);
      } catch {
        ErrorLabel.Visibility = Visibility.Visible;
      }
    }

    #region Border UI Listeners
    private void Border_MouseEnter(object sender, MouseEventArgs e) {
      var border = sender as Border;

      border.BeginAnimation(MarginProperty, MarginShrink);
      border.BeginAnimation(Border.BorderThicknessProperty, BorderExpand);
    }

    private void Border_MouseLeave(object sender, MouseEventArgs e) {
      var border = sender as Border;

      border.BeginAnimation(MarginProperty, MarginExpand);
      border.BeginAnimation(Border.BorderThicknessProperty, BorderShrink);
    }

    private void Border_MouseUp(object sender, MouseButtonEventArgs e) {
      var border = sender as Border;

      current.Effect = null;
      current = border;
      MapSelected();
    }
    #endregion

    public Page Page => this;

    public void Dispose() { }
    public bool HandleMessage(MessageReceivedEventArgs args) => false;
  }
}
