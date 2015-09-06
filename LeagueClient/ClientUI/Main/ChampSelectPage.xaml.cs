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

namespace LeagueClient.ClientUI {
  /// <summary>
  /// Interaction logic for ChampSelect.xaml
  /// </summary>
  public partial class ChampSelectPage : Page, IClientSubPage {
    public event EventHandler Close;

    private const string
      YouPickString = "Your turn to pick!",
      YourTeamBanString = "Your team is banning!",
      OtherTeamBanString = "The other team is banning!",
      NotPickingString = "Please wait your turn",
      YouBanString = "Your turn to ban a champion!",
      PostString = "The game will begin soon";

    private ChatRoomController chatRoom;

    private State state;
    private string header;
    private GameDTO last;

    public ChampSelectPage(GameDTO game) {
      InitializeComponent();
      chatRoom = new ChatRoomController(ChatBox, ChatHistory, ChatButt, ChatScroller);
      chatRoom.JoinChat(new jabber.JID(game.RoomName + ".pvp.net"), game.RoomPassword);

      ChampsGrid.ChampSelected += ChampsGrid_ChampSelected;
      ChampsGrid.SkinSelected += ChampsGrid_SkinSelected;
      GotGameData(game);
      UpdateBooks();

      Popup.SpellSelector.SpellSelected += SpellSelector_SpellSelected;
      Popup.Close += Popup_Close;

      Popup.SpellSelector.Spells = (from spell in LeagueData.SpellData.Value.data.Values
                                    where spell.modes.Contains(game.GameMode)
                                    select spell);

      var map = GameMap.Maps.FirstOrDefault(m => m.MapId == game.MapId);

      var config = Client.LoginPacket.GameTypeConfigs.FirstOrDefault(g => g.Id == game.GameTypeConfigId);
      if(config.MaxAllowableBans == 0) {
        MyTeamBans.Visibility = OtherTeamBans.Visibility = Visibility.Collapsed;
      }

      MapLabel.Content = map.DisplayName;
      ModeLabel.Content = ModeLabel.Content = GameMode.Values[game.GameMode].Value;
      QueueLabel.Content = GameConfig.Values[game.GameTypeConfigId];
      TeamSizeLabel.Content = $"{game.MaxNumPlayers / 2}v{game.MaxNumPlayers / 2}";
    }

    #region RTMP Messages

    public bool HandleMessage(MessageReceivedEventArgs args) {
      PlayerCredentialsDto creds;
      GameDTO game;

      if ((game = args.Body as GameDTO) != null) {
        Dispatcher.MyInvoke(GotGameData, game);
        return true;
      } else if ((creds = args.Body as PlayerCredentialsDto) != null) {
        Close?.Invoke(this, new EventArgs());
        timer.Dispose();
        Client.JoinGame(creds);
      }

      return false;
    }

    public async void GotGameData(GameDTO game) {
      var config = Client.LoginPacket.GameTypeConfigs.FirstOrDefault(q => q.Id == game.GameTypeConfigId);
      if (game.GameState.Equals("CHAMP_SELECT") || game.GameState.Equals("PRE_CHAMP_SELECT")) {
        var turn = Dispatcher.MyInvoke(RenderPlayers, game);
        ChampsGrid.IsReadOnly = !turn.IsMyTurn;

        var me = game.PlayerChampionSelections.FirstOrDefault(p => p.SummonerInternalName == Client.LoginPacket.AllSummonerData.Summoner.InternalName);
        spell1 = LeagueData.GetSpellData(me.Spell1Id);
        spell2 = LeagueData.GetSpellData(me.Spell2Id);
        Dispatcher.Invoke(() => {
          Spell1Image.Source = LeagueData.GetSpellImage(spell1);
          Spell2Image.Source = LeagueData.GetSpellImage(spell2);
        });

        if (game.GameState.Equals("PRE_CHAMP_SELECT")) {
          if (last?.PickTurn != game.PickTurn) SetTimer(config.BanTimerDuration - 3);
          if (turn.IsMyTurn) state = State.Banning;
          else state = State.Watching;
          var champs = await RiotServices.GameService.GetChampionsForBan();

          if (turn.IsOurTurn) {
            ChampsGrid.SetChampList(champs.Where(c => c.EnemyOwned).Select(c => LeagueData.GetChampData(c.ChampionId)));
          } else {
            ChampsGrid.SetChampList(champs.Where(c => c.Owned).Select(c => LeagueData.GetChampData(c.ChampionId)));
          }

          if(turn.IsMyTurn) header = YouBanString;
          else if(turn.IsOurTurn) header = YourTeamBanString;
          else header = OtherTeamBanString;

        } else {
          if (last?.PickTurn != game.PickTurn) SetTimer(config.MainPickTimerDuration - 3);
          if (turn.IsMyTurn) state = State.Picking;
          else state = State.Watching;
          ChampsGrid.UpdateChampList();

          if (turn.IsMyTurn) header = YouPickString;
          else header = NotPickingString;
        }
      } else if(game.GameState.Equals("POST_CHAMP_SELECT")) {
        if (last?.PickTurn != game.PickTurn) SetTimer(config.PostPickTimerDuration - 3);
        var turn = Dispatcher.MyInvoke(RenderPlayers, game);
        state = State.Watching;
        ChampsGrid.IsReadOnly = true;
        header = PostString;
      }
      last = game;
    }

    #endregion

    private void UpdateBooks() {
      RunesBox.ItemsSource = Client.Runes.BookPages;
      RunesBox.SelectedItem = Client.SelectedRunePage;
      MasteriesBox.ItemsSource = Client.Masteries.BookPages;
      MasteriesBox.SelectedItem = Client.SelectedMasteryPage;
    }

    private TurnInfo RenderPlayers(GameDTO game) {
      var turn = new TurnInfo();
      MyTeam.Children.Clear();
      OtherTeam.Children.Clear();
      bool meBlue = game.TeamOne.Any(p => (p as PlayerParticipant)?.AccountId == Client.LoginPacket.AllSummonerData.Summoner.AcctId);
      foreach (var thing in game.TeamOne.Concat(game.TeamTwo)) {
        var player = thing as PlayerParticipant;
        var bot = thing as BotParticipant;
        var obfusc = thing as ObfuscatedParticipant;
        bool blue = game.TeamOne.Contains(player);

        UserControl control;
        if (player != null) {
          control = new ChampSelectPlayer(player, game.PlayerChampionSelections.FirstOrDefault(c => c.SummonerInternalName.Equals(player.SummonerInternalName)));
          if (player.PickTurn == game.PickTurn) {
            if (player.SummonerId == Client.LoginPacket.AllSummonerData.Summoner.SumId) {
              turn.IsMyTurn = turn.IsOurTurn = true;
            } else if (meBlue == blue) {
              turn.IsOurTurn = true;
            }
          }
        } else if (bot != null) {
          control = new ChampSelectPlayer(bot);
        } else if (obfusc != null) {
          control = new ChampSelectPlayer(obfusc);
        } else {
          Client.TryBreak(thing.GetType().Name);
          control = null;
        }

        if (blue) {
          MyTeam.Children.Add(control);
        } else {
          OtherTeam.Children.Add(control);
        }
      }
      MyTeam.Height = OtherTeam.Height = 84 * (game.MaxNumPlayers / 2);

      foreach (var thing in game.BannedChampions) {
        var champ = LeagueData.GetChampData(thing.ChampionId);
        var image = LeagueData.GetChampIconImage(champ);
        switch (thing.PickTurn) {
          case 1: Ban1.Source = image; break;
          case 2: Ban2.Source = image; break;
          case 3: Ban3.Source = image; break;
          case 4: Ban4.Source = image; break;
          case 5: Ban5.Source = image; break;
          case 6: Ban6.Source = image; break;
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

    private void Timer_Elapsed(object sender, ElapsedEventArgs e) {
      try {
        Dispatcher.Invoke(UpdateHeader);
      } catch { timer.Dispose(); }
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
        RiotServices.GameService.SelectChampionSkin(ChampsGrid.SelectedChampion.key, e.id);
    }

    private void LockIn_Click(object sender, RoutedEventArgs e) {
      if (state == State.Picking)
        RiotServices.GameService.ChampionSelectCompleted();
    }

    bool doSpell1;
    SpellDto spell1;
    SpellDto spell2;

    private void Spell1_Click(object sender, MouseButtonEventArgs e) {
      PopupGrid.BeginStoryboard(App.FadeIn);
      Popup.CurrentSelector = PopupSelector.Selector.Spells;
      doSpell1 = true;
    }

    private void Spell2_Click(object sender, MouseButtonEventArgs e) {
      PopupGrid.BeginStoryboard(App.FadeIn);
      Popup.CurrentSelector = PopupSelector.Selector.Spells;
      doSpell1 = false;
    }

    private void MasterEdit_Click(object sender, RoutedEventArgs e) {
      PopupGrid.BeginStoryboard(App.FadeIn);
      Popup.CurrentSelector = PopupSelector.Selector.Masteries;
    }

    private void SpellSelector_SpellSelected(object sender, SpellDto e) {
      PopupGrid.BeginStoryboard(App.FadeOut);
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
      Client.SelectMasteryPage((MasteryBookPageDTO) MasteriesBox.SelectedItem);
      RiotServices.MasteryBookService.SaveMasteryBook(Client.Masteries);
    }

    private void Runes_Selected(object sender, SelectionChangedEventArgs e) {
      if (RunesBox.SelectedIndex < 0) return;
      Client.SelectRunePage((SpellBookPageDTO) RunesBox.SelectedItem);
    }

    private void Popup_Close(object sender, EventArgs e) {
      PopupGrid.BeginStoryboard(App.FadeOut);
      UpdateBooks();
    }

    private void SkinScroller_MouseWheel(object sender, MouseWheelEventArgs e) {
      if (e.Delta > 0)
        ((ScrollViewer) sender).LineLeft();
      else
        ((ScrollViewer) sender).LineRight();
      e.Handled = true;
    }

    private void Border_MouseDown(object sender, MouseButtonEventArgs e) {
      Client.MainWindow.DragMove();
    }

    public void ForceClose() {
      throw new NotImplementedException();
    }

    public IQueuer HandleClose() {
      throw new NotImplementedException();
    }

    #endregion

    public Page Page => this;
    public bool CanPlay => false;
    public bool CanClose => false;

    private enum State {
      Picking, Banning, Watching
    }

    private struct TurnInfo {
      public bool IsMyTurn { get; set; }
      public bool IsOurTurn { get; set; }
    }
  }
}
