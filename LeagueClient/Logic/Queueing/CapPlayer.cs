using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using MFroehlich.League.DataDragon;
using static LeagueClient.Logic.Strings;

namespace LeagueClient.Logic.Cap {
  public class CapPlayer : System.ComponentModel.INotifyPropertyChanged {
    public event PropertyChangedEventHandler PropertyChanged;

    #region Properties
    public int SlotId { get; set; }
    public string Name { get; set; }
    public Duration Timeout {
      get {
        return timeout;
      }
      set {
        if(timeout != value) {
          timeout = value;
          PropertyChanged(this, new PropertyChangedEventArgs(nameof(Timeout)));
        }
        TimeoutStart = DateTime.Now;
      }
    }
    public DateTime TimeoutStart { get; private set; }

    public ChampionDto Champion {
      get { return champion; }
      set {
        if(champion != value) {
          champion = value;
          PropertyChanged(this, new PropertyChangedEventArgs(nameof(champion)));
        }
      }
    }
    public SpellDto Spell1 {
      get { return spell1; }
      set {
        if (spell1 != value) {
          spell1 = value;
          PropertyChanged(this, new PropertyChangedEventArgs(nameof(spell1)));
        }
      }
    }
    public SpellDto Spell2 {
      get { return spell2; }
      set {
        if (spell2 != value) {
          spell2 = value;
          PropertyChanged(this, new PropertyChangedEventArgs(nameof(spell2)));
        }
      }
    }

    public Position Position {
      get { return position; }
      set {
        if (position != value) {
          position = value;
          PropertyChanged(this, new PropertyChangedEventArgs(nameof(position)));
        }
      }
    }
    public Role Role {
      get { return role; }
      set {
        if (role != value) {
          role = value;
          PropertyChanged(this, new PropertyChangedEventArgs(nameof(role)));
        }
      }
    }

    public CapStatus Status {
      get { return status; }
      set {
        if (status != value) {
          status = value;
          PropertyChanged(this, new PropertyChangedEventArgs(nameof(status)));
        }
      }
    }
    #endregion

    private ChampionDto champion;
    private SpellDto spell1;
    private SpellDto spell2;

    private Position position;
    private Role role;
    private CapStatus status;
    private Duration timeout;

    public bool CanBeReady() {
      return !(Champion == null || Spell1 == null || Spell2 == null || Position == null || Role == null);
    }
  }

  public enum CapStatus {
    Present, Ready, Searching, Choosing, Maybe, Penalty, SearchingDeclined
  }
}
