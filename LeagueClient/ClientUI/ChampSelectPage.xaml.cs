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
using LeagueClient.ClientUI.Controls;
using LeagueClient.ClientUI.Main;
using LeagueClient.Logic;
using LeagueClient.Logic.Chat;
using LeagueClient.Logic.Queueing;
using LeagueClient.Logic.Riot;
using LeagueClient.Logic.Riot.Platform;
using MFroehlich.League.Assets;
using MFroehlich.League.DataDragon;
using RtmpSharp.Messaging;
using agsXMPP;

namespace LeagueClient.ClientUI {
  /// <summary>
  /// Interaction logic for ChampSelect.xaml
  /// </summary>
  public sealed partial class ChampSelectPage : Page, IClientPage, IDisposable {
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

    public ChampSelectPage() {
      InitializeComponent();
    }

    public ChampSelectPage(GameDTO game) : this() {
      chatRoom = new ChatRoom(ChatBox, ChatHistory, ChatButt, ChatScroller);
      chatRoom.JoinChat(new Jid(game.RoomName.ToLower() + ".pvp.net"), game.RoomPassword);

      Popup.ChampSelector.ChampSelected += ChampsGrid_ChampSelected;
      Popup.ChampSelector.SkinSelected += ChampsGrid_SkinSelected;
      GotGameData(game);
      UpdateBooks();

      Popup.SpellSelector.SpellSelected += SpellSelector_SpellSelected;
      Popup.Close += Popup_Close;

      Popup.SpellSelector.Spells = (from spell in LeagueData.SpellData.Value.data.Values
                                    where spell.modes.Contains(game.GameMode)
                                    select spell);

      var map = GameMap.Maps.FirstOrDefault(m => m.MapId == game.MapId);

      var config = Client.Session.LoginPacket.GameTypeConfigs.FirstOrDefault(g => g.Id == game.GameTypeConfigId);
      if (config.MaxAllowableBans == 0) {
        MyBansGrid.Visibility = OtherBansGrid.Visibility = Visibility.Collapsed;
      }

      MapLabel.Content = map.DisplayName;
      ModeLabel.Content = ModeLabel.Content = GameMode.Values[game.GameMode].Value;
      QueueLabel.Content = GameConfig.Values[game.GameTypeConfigId];
      TeamSizeLabel.Content = $"{game.MaxNumPlayers / 2}v{game.MaxNumPlayers / 2}";
    }

    #region RTMP Messages

    bool IClientPage.HandleMessage(MessageReceivedEventArgs args) {
      PlayerCredentialsDto creds;
      GameDTO game;

      if ((game = args.Body as GameDTO) != null) {
        Dispatcher.MyInvoke(GotGameData, game);
        return true;
      } else if ((creds = args.Body as PlayerCredentialsDto) != null) {
        ChampSelectCompleted?.Invoke(this, new EventArgs());
        Client.Session.Credentials = creds;
        Client.Session.JoinGame();
        Dispose();
        return true;
      }

      return false;
    }

    public async void GotGameData(GameDTO game) {
      var config = Client.Session.LoginPacket.GameTypeConfigs.FirstOrDefault(q => q.Id == game.GameTypeConfigId);
      LockInButt.IsEnabled = false;
      if (game.GameState.Equals("CHAMP_SELECT") || game.GameState.Equals("PRE_CHAMP_SELECT")) {
        var turn = Dispatcher.MyInvoke(RenderPlayers, game);
        Popup.ChampSelector.IsReadOnly = !turn.IsMyTurn;

        var me = game.PlayerChampionSelections.FirstOrDefault(p => p.SummonerInternalName == Client.Session.LoginPacket.AllSummonerData.Summoner.InternalName);
        spell1 = LeagueData.GetSpellData(me.Spell1Id);
        spell2 = LeagueData.GetSpellData(me.Spell2Id);
        Dispatcher.Invoke(() => {
          Spell1Image.Source = LeagueData.GetSpellImage(spell1);
          Spell2Image.Source = LeagueData.GetSpellImage(spell2);
        });

        LockInButt.IsEnabled = turn.IsMyTurn;

        if (game.GameState.Equals("PRE_CHAMP_SELECT")) {
          if (last?.PickTurn != game.PickTurn) SetTimer(config.BanTimerDuration - 2);
          if (turn.IsMyTurn) state = State.Banning;
          else state = State.Watching;
          var champs = await RiotServices.GameService.GetChampionsForBan();

          if (turn.IsOurTurn) {
            Popup.ChampSelector.SetChampList(champs.Where(c => c.EnemyOwned).Select(c => LeagueData.GetChampData(c.ChampionId)));
          } else {
            Popup.ChampSelector.SetChampList(champs.Where(c => c.Owned).Select(c => LeagueData.GetChampData(c.ChampionId)));
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
      } else if (game.GameState.Equals("TEAM_SELECT")) {
        Dispatcher.Invoke(() => {
          var page = new CustomLobbyPage(game);
          Client.Session.QueueManager.ShowPage(page);
        });
      } else {

      }

      MyTeam.Columns = OtherTeam.Columns = game.MaxNumPlayers / 2;
      UpdateHeader();
      last = game;
    }

    #endregion

    private void UpdateBooks() {
      RunesBox.ItemsSource = Client.Session.Runes.BookPages;
      RunesBox.SelectedItem = Client.Session.SelectedRunePage;
      MasteriesBox.ItemsSource = Client.Session.Masteries.BookPages;
      MasteriesBox.SelectedItem = Client.Session.SelectedMasteryPage;
    }

    private TurnInfo RenderPlayers(GameDTO game) {
      var turn = new TurnInfo();
      MyTeam.Children.Clear();
      OtherTeam.Children.Clear();
      bool meBlue = game.TeamOne.Any(p => (p as PlayerParticipant)?.AccountId == Client.Session.LoginPacket.AllSummonerData.Summoner.AccountId);
      foreach (var thing in game.TeamOne.Concat(game.TeamTwo)) {
        var player = thing as PlayerParticipant;
        var bot = thing as BotParticipant;
        var obfusc = thing as ObfuscatedParticipant;
        bool blue = game.TeamOne.Contains(thing);

        UserControl control;
        var selection = game.PlayerChampionSelections.FirstOrDefault(c => c.SummonerInternalName == player.SummonerInternalName);
        if (player != null) {
          control = new ChampSelectPlayer(player, selection);
          if (player.PickTurn == game.PickTurn) {
            if (player.SummonerId == Client.Session.LoginPacket.AllSummonerData.Summoner.SummonerId) {
              turn.IsMyTurn = turn.IsOurTurn = true;
            } else if (meBlue == blue) {
              turn.IsOurTurn = true;
            }
          }
        } else if (bot != null) {
          control = new ChampSelectPlayer(bot);
        } else if (obfusc != null) {
          control = new ChampSelectPlayer(obfusc, selection);
        } else {
          Client.Log(thing.GetType().Name);
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
      Image[] myBans, otherBans;
      if (meBlue) {
        myBans = new[] { Ban1, Ban2, Ban3 };
        otherBans = new[] { Ban4, Ban5, Ban6 };
      } else {
        myBans = new[] { Ban4, Ban5, Ban6 };
        otherBans = new[] { Ban1, Ban2, Ban3 };
      }
      foreach (var thing in game.BannedChampions) {
        var champ = LeagueData.GetChampData(thing.ChampionId);
        var image = LeagueData.GetChampIconImage(champ);
        if (thing.PickTurn % 2 == 0) {
          //1, 3, 5
          otherBans[thing.PickTurn / 2].Source = image;
        } else {
          //2, 4, 6
          myBans[thing.PickTurn / 2].Source = image;
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

    private void Border_MouseDown(object sender, MouseButtonEventArgs e) => Client.MainWindow.DragMove();

    private void BackButton_Click(object sender, RoutedEventArgs e) => Client.MainWindow.ShowLandingPage();

    private void Timer_Elapsed(object sender, ElapsedEventArgs e) {
      try {
        Dispatcher.Invoke(UpdateHeader);
      } catch { Dispose(); }
    }

    private void ChampsGrid_ChampSelected(object sender, ChampionDto e) {
      switch (state) {
        case State.Watching: throw new InvalidOperationException("Attempted to select a champion out of turn");
        case State.Picking:
          RiotServices.GameService.SelectChampion(e.key);
          break;
        case State.Banning:
          RiotServices.GameService.BanChampion(e.key);
          break;
      }
    }

    private void ChampsGrid_SkinSelected(object sender, ChampionDto.SkinDto e) {
      if (state != State.Banning)
        RiotServices.GameService.SelectChampionSkin(Popup.ChampSelector.SelectedChampion.key, e.id);
    }

    private void GameStatusLabel_MouseUp(object sender, MouseButtonEventArgs e) {
      Popup.BeginStoryboard(App.FadeIn);
      Popup.CurrentSelector = PopupSelector.Selector.Champions;
    }

    private void LockIn_Click(object sender, RoutedEventArgs e) {
      if (state == State.Picking)
        RiotServices.GameService.ChampionSelectCompleted();
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
        Spell1Image.Source = LeagueData.GetSpellImage(e);
      } else {
        spell2 = e;
        Spell2Image.Source = LeagueData.GetSpellImage(e);
      }
      RiotServices.GameService.SelectSpells(spell1.key, spell2.key);
    }

    private void Masteries_Selected(object sender, SelectionChangedEventArgs e) {
      if (MasteriesBox.SelectedIndex < 0) return;
      Client.Session.SelectMasteryPage((MasteryBookPageDTO) MasteriesBox.SelectedItem);
      RiotServices.MasteryBookService.SaveMasteryBook(Client.Session.Masteries);
    }

    private void Runes_Selected(object sender, SelectionChangedEventArgs e) {
      if (RunesBox.SelectedIndex < 0) return;
      Client.Session.SelectRunePage((SpellBookPageDTO) RunesBox.SelectedItem);
    }

    private void Popup_Close(object sender, EventArgs e) {
      Popup.BeginStoryboard(App.FadeOut);
      UpdateBooks();
    }

    #endregion

    public void Dispose() {
      timer.Dispose();
      chatRoom.Dispose();
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
