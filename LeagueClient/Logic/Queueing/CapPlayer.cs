using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using MFroehlich.League.DataDragon;
using static LeagueClient.Logic.Strings;

namespace LeagueClient.Logic.Cap {
  public class CapPlayer {
    public event EventHandler PlayerUpdate;

    #region Properties
    public int SlotId { get; set; }
    public string Name { get; set; }
    public Duration Timeout {
      get {
        return timeout;
      }
      set {
        timeout = value;
        TimeoutStart = DateTime.Now;
      }
    }
    public DateTime TimeoutStart { get; private set; }

    public ChampionDto Champion {
      get { return champion; }
      set {
        champion = value;
        if(PlayerUpdate != null) PlayerUpdate(this, new EventArgs());
      }
    }
    public SpellDto Spell1 {
      get { return spell1; }
      set {
        spell1 = value;
        if (PlayerUpdate != null) PlayerUpdate(this, new EventArgs());
      }
    }
    public SpellDto Spell2 {
      get { return spell2; }
      set {
        spell2 = value;
        if (PlayerUpdate != null) PlayerUpdate(this, new EventArgs());
      }
    }

    public Position Position {
      get { return position; }
      set {
        position = value;
        if (PlayerUpdate != null) PlayerUpdate(this, new EventArgs());
      }
    }
    public Role Role {
      get { return role; }
      set {
        role = value;
        if (PlayerUpdate != null) PlayerUpdate(this, new EventArgs());
      }
    }

    public CapStatus Status {
      get { return status; }
      set {
        status = value;
        if (PlayerUpdate != null) PlayerUpdate(this, new EventArgs());
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
