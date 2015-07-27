using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MFroehlich.League.DataDragon;
using static LeagueClient.Logic.Strings;

namespace LeagueClient.Logic.Cap {
  public class CapPlayer {
    public event EventHandler PlayerUpdate;

    #region Properties
    public ChampionDto Champion {
      get { return champion; }
      set {
        champion = value;
        PlayerUpdate(this, new EventArgs());
      }
    }
    public SpellDto Spell1 {
      get { return spell1; }
      set {
        spell1 = value;
        PlayerUpdate(this, new EventArgs());
      }
    }
    public SpellDto Spell2 {
      get { return spell2; }
      set {
        spell2 = value;
        PlayerUpdate(this, new EventArgs());
      }
    }
    public ChampionDto.SkinDto Skin {
      get { return skin; }
      set {
        skin = value;
        PlayerUpdate(this, new EventArgs());
      }
    }

    public Position Position {
      get { return position; }
      set {
        position = value;
        PlayerUpdate(this, new EventArgs());
      }
    }
    public Role Role {
      get { return role; }
      set {
        role = value;
        PlayerUpdate(this, new EventArgs());
      }
    }
    public bool Ready {
      get { return ready; }
      set {
        ready = value;
        PlayerUpdate(this, new EventArgs());
      }
    }
    #endregion

    private ChampionDto champion;
    private SpellDto spell1;
    private SpellDto spell2;
    private ChampionDto.SkinDto skin;

    private Position position;
    private Role role;
    private bool ready;

    public bool CanBeReady() {
      return !(Champion == null || Spell1 == null || Spell2 == null || Skin == null || Position == null || Role == null);
    }
  }
}
