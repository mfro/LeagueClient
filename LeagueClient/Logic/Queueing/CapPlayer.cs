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
  public class CapPlayer {
    public event EventHandler<CapPlayerEventArgs> CapEvent;

    public void ChangeProperty(string name, object value) => CapEvent?.Invoke(this, new PropertyChangedEventArgs(name, value));
    public void Accept(bool accept) => CapEvent?.Invoke(this, new CandidateAcceptedEventArgs(accept));
    public void GiveInvite() => CapEvent?.Invoke(this, new GiveInviteEventArgs());
    public void Kick() => CapEvent?.Invoke(this, new KickedEventArgs());

    public CapPlayer(int slotId) {
      SlotId = slotId;
    }

    public int SlotId { get; set; }
    public string Name { get; set; }
    public DateTime TimeoutEnd { get; set; }

    public ChampionDto Champion { get; set; }
    public SpellDto Spell1 { get; set; }
    public SpellDto Spell2 { get; set; }

    public Position Position { get; set; }
    public Role Role { get; set; }

    public CapStatus Status { get; set; }

    public void ClearPlayerData() {
      Champion = null;
      Spell1 = Spell2 = null;
      Name = null;
    }

    public bool CanBeReady() => !(Champion == null || Spell1 == null || Spell2 == null || Position == null || Role == null);
  }

  public abstract class CapPlayerEventArgs : EventArgs {

  }

  public class CandidateAcceptedEventArgs : CapPlayerEventArgs {
    public bool Accepted { get; }
    public CandidateAcceptedEventArgs(bool accepted) {
      Accepted = accepted;
    }
  }

  public class PropertyChangedEventArgs : CapPlayerEventArgs {
    public string PropertyName { get; }
    public object Value { get; }

    public PropertyChangedEventArgs(string name, object value) {
      PropertyName = name;
      Value = value;
    }
  }

  public class GiveInviteEventArgs : CapPlayerEventArgs { }
  public class KickedEventArgs : CapPlayerEventArgs { }


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
    /// <summary>
    /// Populated slot before search for solo players has begun
    /// </summary>
    Choosing
  }
}
