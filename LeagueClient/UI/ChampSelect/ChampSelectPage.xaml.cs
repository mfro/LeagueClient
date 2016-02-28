using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using LeagueClient.Logic;
using LeagueClient.Logic.Chat;
using LeagueClient.Logic.Queueing;
using MFroehlich.League.Assets;
using MFroehlich.League.DataDragon;
using RtmpSharp.Messaging;
using agsXMPP;
using LeagueClient.UI.Client.Custom;
using LeagueClient.UI.Selectors;
using RiotClient.Lobbies;
using RiotClient.Riot.Platform;
using RiotClient;

namespace LeagueClient.UI.ChampSelect {
  /// <summary>
  /// Interaction logic for ChampSelect.xaml
  /// </summary>
  public sealed partial class ChampSelectPage : Page, IDisposable {
    public event EventHandler ChampSelectCompleted;

    private const string
      YouPickString = "Your turn to pick!",
      YourTeamBanString = "Your team is banning!",
      OtherTeamBanString = "The other team is banning!",
      NotPickingString = "Please wait your turn",
      YouBanString = "Your turn to ban a champion!",
      PostString = "The game will begin soon";

    private ChatRoom chatRoom;

    private State state;
    private string header;
    private GameDTO last;
    private GameLobby game;

    public ChampSelectPage(GameLobby game) {
      InitializeComponent();

      this.game = game;

      game.GameCancelled += Game_LeftChampSelect;
      game.GameStarted += Game_GameStarted;
      game.Updated += Updated;
      game.Loaded += Game_Loaded;
      Update();

      Popup.ChampSelector.ChampSelected += ChampsGrid_ChampSelected;
      Popup.ChampSelector.SkinSelected += ChampsGrid_SkinSelected;
      UpdateBooks();

      Popup.SpellSelector.SpellSelected += SpellSelector_SpellSelected;
      Popup.Close += Popup_Close;

      Popup.SpellSelector.Spells = (from spell in DataDragon.SpellData.Value.data.Values
                                    where spell.modes.Contains(game.Data.GameMode)
                                    select spell);

      var map = GameMap.Maps.FirstOrDefault(m => m.MapId == game.Data.MapId);

      var config = Session.Current.Account.LoginPacket.GameTypeConfigs.FirstOrDefault(g => g.Id == game.Data.GameTypeConfigId);
      if (config.MaxAllowableBans == 0) {
        MyBansGrid.Visibility = OtherBansGrid.Visibility = Visibility.Collapsed;
      }

      MapLabel.Content = map.DisplayName;
      ModeLabel.Content = ModeLabel.Content = GameMode.Values[game.Data.GameMode].Value;
      QueueLabel.Content = GameConfig.Values[game.Data.GameTypeConfigId];
      TeamSizeLabel.Content = $"{game.Data.MaxNumPlayers / 2}v{game.Data.MaxNumPlayers / 2}";
    }

    #region RTMP Messages

    private void Game_Loaded(object sender, EventArgs e) {
      chatRoom = new ChatRoom(game.ChatLobby, ChatBox, ChatHistory, ChatButt, ChatScroller);
    }

    private void Updated(object sender, EventArgs e) => Dispatcher.Invoke(Update);

    private void Game_LeftChampSelect(object sender, object e) {
      Dispatcher.Invoke(() => {
        ChampSelectCompleted?.Invoke(this, new EventArgs());
        Dispose();
        if (e is CustomLobby) {
          LoLClient.QueueManager.ShowPage(new CustomLobbyPage(e as CustomLobby));
        } else {

        }
      });
    }

    private void Game_GameStarted(object sender, PlayerCredentialsDto creds) {
      ChampSelectCompleted?.Invoke(this, new EventArgs());
      Session.Current.Credentials = creds;
      Session.Current.JoinGame();
      Dispose();
    }

    private async void Update() {
      var game = this.game.Data;
      var config = Session.Current.Account.LoginPacket.GameTypeConfigs.FirstOrDefault(q => q.Id == game.GameTypeConfigId);
      LockInButt.IsEnabled = false;
      var myChamp = game.PlayerChampionSelections
        .FirstOrDefault(p => p.SummonerInternalName == Session.Current.Account.LoginPacket.AllSummonerData.Summoner.InternalName);
      var me = (PlayerParticipant) game.TeamOne.Concat(game.TeamTwo)
        .FirstOrDefault(p => (p as PlayerParticipant)?.AccountId == Session.Current.Account.AccountID);

      if (game.GameState.Equals("CHAMP_SELECT") || game.GameState.Equals("PRE_CHAMP_SELECT")) {
        var turn = Dispatcher.MyInvoke(RenderPlayers, game);
        Popup.ChampSelector.IsReadOnly = !turn.IsMyTurn;

        spell1 = DataDragon.GetSpellData(myChamp.Spell1Id);
        spell2 = DataDragon.GetSpellData(myChamp.Spell2Id);
        Dispatcher.Invoke(() => {
          Spell1Image.Source = DataDragon.GetSpellImage(spell1).Load();
          Spell2Image.Source = DataDragon.GetSpellImage(spell2).Load();
        });

        LockInButt.IsEnabled = turn.IsMyTurn;

        if (game.GameState.Equals("PRE_CHAMP_SELECT")) {
          if (last?.PickTurn != game.PickTurn) SetTimer(config.BanTimerDuration - 2);
          if (turn.IsMyTurn) state = State.Banning;
          else state = State.Watching;
          var champs = await this.game.GetChampionsForBan();

          if (turn.IsOurTurn) {
            Popup.ChampSelector.SetChampList(champs.Where(c => c.EnemyOwned).Select(c => DataDragon.GetChampData(c.ChampionId)));
          } else {
            Popup.ChampSelector.SetChampList(champs.Where(c => c.Owned).Select(c => DataDragon.GetChampData(c.ChampionId)));
          }

          if (turn.IsMyTurn) header = YouBanString;
          else if (turn.IsOurTurn) header = YourTeamBanString;
          else header = OtherTeamBanString;

        } else {
          if (last?.PickTurn != game.PickTurn) SetTimer(config.MainPickTimerDuration - 2);
          if (turn.IsMyTurn) state = State.Picking;
          else state = State.Watching;
          Popup.ChampSelector.UpdateChampList();

          if (turn.IsMyTurn) header = YouPickString;
          else header = NotPickingString;
        }
      } else if (game.GameState.Equals("POST_CHAMP_SELECT")) {
        if (last?.PickTurn != game.PickTurn) SetTimer(config.PostPickTimerDuration - 2);
        var turn = Dispatcher.MyInvoke(RenderPlayers, game);
        state = State.Watching;
        Popup.ChampSelector.IsReadOnly = true;
        header = PostString;
      } else {

      }

      if (game.GameType == GameConfig.AllRandom.Value) {
        LockInButt.Content = $"{me.PointSummary.NumberOfRolls} / {me.PointSummary.MaxRolls}";
        LockInButt.IsEnabled = me.PointSummary.NumberOfRolls > 0;
      }

      MyTeam.Columns = OtherTeam.Columns = game.MaxNumPlayers / 2;
      UpdateHeader();
      last = game;
    }

    #endregion

    private void UpdateBooks() {
      RunesBox.ItemsSource = Session.Current.Account.Runes.BookPages;
      RunesBox.SelectedItem = Session.Current.Account.SelectedRunePage;
      MasteriesBox.ItemsSource = Session.Current.Account.Masteries.BookPages;
      MasteriesBox.SelectedItem = Session.Current.Account.SelectedMasteryPage;
    }

    private TurnInfo RenderPlayers(GameDTO game) {
      var turn = new TurnInfo();
      MyTeam.Children.Clear();
      OtherTeam.Children.Clear();
      bool meBlue = game.TeamOne.Any(p => (p as PlayerParticipant)?.AccountId == Session.Current.Account.AccountID);
      foreach (var thing in game.TeamOne.Concat(game.TeamTwo)) {
        var player = thing as PlayerParticipant;
        var bot = thing as BotParticipant;
        var obfusc = thing as ObfuscatedParticipant;
        bool blue = game.TeamOne.Contains(thing);

        UserControl control;
        if (player != null) {
          var selection = game.PlayerChampionSelections?.FirstOrDefault(c => c.SummonerInternalName == player.SummonerInternalName);
          control = new ChampSelectPlayer(player, selection);
          if (player.PickTurn == game.PickTurn) {
            if (player.SummonerId == Session.Current.Account.SummonerID) {
              turn.IsMyTurn = turn.IsOurTurn = true;
            } else if (meBlue == blue) {
              turn.IsOurTurn = true;
            }
          }
        } else if (bot != null) {
          control = new ChampSelectPlayer(bot);
        } else if (obfusc != null) {
          control = new ChampSelectPlayer(obfusc, null);
        } else {
          Session.Log(thing.GetType().Name);
          control = null;
        }

        if (blue == meBlue) {
          MyTeam.Children.Add(control);
        } else {
          OtherTeam.Children.Add(control);
        }
      }
      if (OtherTeam.Children.Count == 0) OtherTeam.Visibility = Visibility.Collapsed;

      Ban1.Source = Ban2.Source = Ban3.Source = Ban4.Source = Ban5.Source = Ban6.Source = null;
      Image[] blueBans, redBans;
      if (meBlue) {
        blueBans = new[] { Ban1, Ban2, Ban3 };
        redBans = new[] { Ban4, Ban5, Ban6 };
      } else {
        blueBans = new[] { Ban4, Ban5, Ban6 };
        redBans = new[] { Ban1, Ban2, Ban3 };
      }
      foreach (var thing in game.BannedChampions) {
        var champ = DataDragon.GetChampData(thing.ChampionId);
        var image = DataDragon.GetChampIconImage(champ);
        int index = thing.PickTurn - 1;
        if (index % 2 == 0) {
          //0, 2, 4: Blue team's bans
          blueBans[thing.PickTurn / 2].Source = image.Load();
        } else {
          //1, 3, 5: Red team's bans
          redBans[thing.PickTurn / 2].Source = image.Load();
        }
      }

      //Dispatcher.BeginInvoke((Action) (() => MyTeam.Height = OtherTeam.Height = Math.Max(MyTeam.ActualHeight, OtherTeam.ActualHeight)), System.Windows.Threading.DispatcherPriority.Input);
      return turn;
    }

    private int counter;
    private Timer timer;

    private void SetTimer(int time) {
      timer?.Dispose();

      counter = time;
      timer = new Timer(1000);
      timer.Elapsed += Timer_Elapsed;
      timer.Start();
      UpdateHeader();
    }

    private void UpdateHeader() {
      if (counter > 0) GameStatusLabel.Content = header + "  " + counter--;
      else GameStatusLabel.Content = header;
    }

    #region Event Listeners

    private void Border_MouseDown(object sender, MouseButtonEventArgs e) => LoLClient.MainWindow.DragMove();

    private void BackButton_Click(object sender, RoutedEventArgs e) => LoLClient.MainWindow.ShowLandingPage();

    private void Timer_Elapsed(object sender, ElapsedEventArgs e) {
      try {
        Dispatcher.Invoke(UpdateHeader);
      } catch { Dispose(); }
    }

    private void ChampsGrid_ChampSelected(object sender, ChampionDto e) {
      switch (state) {
        case State.Watching: throw new InvalidOperationException("Attempted to select a champion out of turn");
        case State.Picking:
          game.SelectChampion(e.key);
          break;
        case State.Banning:
          game.BanChampion(e.key);
          break;
      }
    }

    private void ChampsGrid_SkinSelected(object sender, ChampionDto.SkinDto e) {
      if (state != State.Banning)
        game.SelectSkin(Popup.ChampSelector.SelectedChampion.key, e.id);
    }

    private void GameStatusLabel_MouseUp(object sender, MouseButtonEventArgs e) {
      Popup.BeginStoryboard(App.FadeIn);
      Popup.CurrentSelector = PopupSelector.Selector.Champions;
    }

    private void LockIn_Click(object sender, RoutedEventArgs e) {
      if (state == State.Picking)
        game.LockIn();
    }

    bool doSpell1;
    SpellDto spell1;
    SpellDto spell2;

    private void Spell1_Click(object sender, MouseButtonEventArgs e) {
      Popup.BeginStoryboard(App.FadeIn);
      Popup.CurrentSelector = PopupSelector.Selector.Spells;
      doSpell1 = true;
    }

    private void Spell2_Click(object sender, MouseButtonEventArgs e) {
      Popup.BeginStoryboard(App.FadeIn);
      Popup.CurrentSelector = PopupSelector.Selector.Spells;
      doSpell1 = false;
    }

    private void MasteryEdit_Click(object sender, RoutedEventArgs e) {
      Popup.BeginStoryboard(App.FadeIn);
      Popup.CurrentSelector = PopupSelector.Selector.Masteries;
    }

    private void RuneEdit_Click(object sender, RoutedEventArgs e) {
      Popup.BeginStoryboard(App.FadeIn);
      Popup.CurrentSelector = PopupSelector.Selector.Runes;
    }

    private void SpellSelector_SpellSelected(object sender, SpellDto e) {
      Popup.BeginStoryboard(App.FadeOut);
      if (doSpell1) {
        spell1 = e;
        Spell1Image.Source = DataDragon.GetSpellImage(e).Load();
      } else {
        spell2 = e;
        Spell2Image.Source = DataDragon.GetSpellImage(e).Load();
      }
      game.SelectSpells(spell1.key, spell2.key);
    }

    private void Masteries_Selected(object sender, SelectionChangedEventArgs e) {
      if (MasteriesBox.SelectedIndex < 0) return;
      Session.Current.Account.SelectMasteryPage((MasteryBookPageDTO) MasteriesBox.SelectedItem);
    }

    private void Runes_Selected(object sender, SelectionChangedEventArgs e) {
      if (RunesBox.SelectedIndex < 0) return;
      Session.Current.Account.SelectRunePage((SpellBookPageDTO) RunesBox.SelectedItem);
    }

    private void Popup_Close(object sender, EventArgs e) {
      Popup.BeginStoryboard(App.FadeOut);
      UpdateBooks();
    }

    #endregion

    public void Dispose() {
      timer.Dispose();

      game.GameCancelled -= Game_LeftChampSelect;
      game.GameStarted -= Game_GameStarted;
      game.Updated -= Updated;
      game.Loaded -= Game_Loaded;
      //chatRoom.Dispose();
    }

    private enum State {
      Picking, Banning, Watching
    }

    private struct TurnInfo {
      public bool IsMyTurn { get; set; }
      public bool IsOurTurn { get; set; }
    }
  }
}
