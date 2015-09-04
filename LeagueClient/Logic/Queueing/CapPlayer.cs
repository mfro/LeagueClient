using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using MFroehlich.League.Assets;
using MFroehlich.League.DataDragon;

namespace LeagueClient.Logic.Cap {
  public class CapPlayer : System.ComponentModel.INotifyPropertyChanged {
    public event PropertyChangedEventHandler PropertyChanged;

    public CapPlayer(int slotId) {
      SlotId = slotId;
    }

    #region Properties
    public int SlotId { get; set; }
    public string Name { get; set; }
    public DateTime TimeoutEnd {
      get { return timeoutEnd; }
      set {
        SetField(ref timeoutEnd, value);
      }
    }

    public ChampionDto Champion {
      get { return champion; }
      set { SetField(ref champion, value); }
    }
    public SpellDto Spell1 {
      get { return spell1; }
      set { SetField(ref spell1, value); }
    }
    public SpellDto Spell2 {
      get { return spell2; }
      set { SetField(ref spell2, value); }
    }

    public Position Position {
      get { return position; }
      set { SetField(ref position, value); }
    }
    public Role Role {
      get { return role; }
      set { SetField(ref role, value); }
    }

    public CapStatus Status {
      get { return status; }
      set { SetField(ref status, value); }
    }

    public BitmapImage ChampionImage => Champion != null ? LeagueData.GetChampIconImage(Champion.id) : null;
    public BitmapImage Spell1Image => Spell1 != null ? LeagueData.GetSpellImage(Spell1.id) : null;
    public BitmapImage Spell2Image => Spell2 != null ? LeagueData.GetSpellImage(Spell2.id) : null;
    #endregion

    private ChampionDto champion;
    private SpellDto spell1;
    private SpellDto spell2;

    private Position position;
    private Role role;
    private CapStatus status;
    private DateTime timeoutEnd;

    public void ClearPlayerData() {
      Champion = null;
      Spell1 = Spell2 = null;
      Name = null;
    }

    private void SetField<T>(ref T field, T value, [System.Runtime.CompilerServices.CallerMemberName] string name = null) {
      if(!(field?.Equals(value) ?? false)) {
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        if (GetType().GetProperty(name + "Image") != null)
          PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name + "Image"));
      }
    }

    public bool CanBeReady() {
      return !(Champion == null || Spell1 == null || Spell2 == null || Position == null || Role == null);
    }
  }

  public enum CapStatus {
    /// <summary>
    /// Empty slot that potentially has role and position filled with advertisements that is waiting to search
    /// </summary>
    ChoosingAdvert,
    /// <summary>
    /// Empty slot that has role and position filled with advertisements that is trying to be filled
    /// </summary>
    Searching,
    /// <summary>
    /// Kinda full slot that has champion, role, and position. Occurs when a candidate is found
    /// </summary>
    Found,
    /// <summary>
    /// After a candidate has been accepted, waiting for them to join the lobby
    /// </summary>
    Joining,
    /// <summary>
    /// Entirely in the lobby
    /// </summary>
    Present,
    /// <summary>
    /// Same as Present but ready
    /// </summary>
    Ready,
    /// <summary>
    /// Inactive slot that is disabled for a set amount of time before becoming searching
    /// </summary>
    Penalty,
    /// <summary>
    /// Same functionally as Searching but after a player has left the slot
    /// </summary>
    SearchingDeclined,
  }
}
