using RiotClient.Riot.Platform;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RiotClient {
  public class Account {
    public LoginDataPacket LoginPacket { get; }
    public LoginSession LoginSession { get; }

    public int ProfileIconID => LoginPacket.AllSummonerData.Summoner.ProfileIconId;
    public long SummonerID => LoginPacket.AllSummonerData.Summoner.SummonerId;
    public long AccountID => LoginPacket.AllSummonerData.Summoner.AccountId;
    public int Level => LoginPacket.AllSummonerData.SummonerLevel.Level;
    public string Name => LoginPacket.AllSummonerData.Summoner.Name;

    public int IP => LoginPacket.IpBalance;
    public int RP => LoginPacket.RpBalance;

    public SpellBookDTO Runes => LoginPacket.AllSummonerData.SpellBook;
    public MasteryBookDTO Masteries => LoginPacket.AllSummonerData.MasteryBook;
    public SpellBookPageDTO SelectedRunePage { get; private set; }
    public MasteryBookPageDTO SelectedMasteryPage { get; private set; }

    internal Account(LoginSession session, LoginDataPacket packet) {
      LoginSession = session;
      LoginPacket = packet;

      SelectedRunePage = Runes.BookPages.FirstOrDefault(p => p.Current);
      SelectedMasteryPage = Masteries.BookPages.FirstOrDefault(p => p.Current);
    }

    #region | Runes and Masteries |

    /// <summary>
    /// Selects a rune page as the default selected page for your account and
    /// updates the contents of the local and server-side spell books
    /// </summary>
    /// <param name="page">The page to select</param>
    public async void SelectRunePage(SpellBookPageDTO page) {
      if (page == SelectedRunePage) return;

      foreach (var item in Runes.BookPages) item.Current = false;
      page.Current = true;
      SelectedRunePage = page;
      await RiotServices.SpellBookService.SelectDefaultSpellBookPage(page);
      await RiotServices.SpellBookService.SaveSpellBook(Runes);
    }

    /// <summary>
    /// Selects a mastery page as the default selected page for your account and
    /// updates the contents of the local and server-side mastery books
    /// </summary>
    /// <param name="page">The page to select</param>
    public async void SelectMasteryPage(MasteryBookPageDTO page) {
      if (page == SelectedMasteryPage) return;

      foreach (var item in Masteries.BookPages) item.Current = false;
      page.Current = true;
      SelectedMasteryPage = page;
      await RiotServices.MasteryBookService.SelectDefaultMasteryBookPage(page);
      await RiotServices.MasteryBookService.SaveMasteryBook(Masteries);
    }
    /// <summary>
    /// Deletes a mastery page from your mastery page book and updates the
    /// contents of the local and server-side mastery books
    /// </summary>
    /// <param name="page">The page to delete</param>
    public void DeleteMasteryPage(MasteryBookPageDTO page) {
      if (!Masteries.BookPages.Contains(page)) throw new ArgumentException("Book page not found: " + page);
      Masteries.BookPages.Remove(page);
      SelectedMasteryPage = Masteries.BookPages.First();
      SelectedMasteryPage.Current = true;
      RiotServices.MasteryBookService.SaveMasteryBook(Masteries);
    }

    public void SaveRunes() {
      RiotServices.SpellBookService.SaveSpellBook(Runes);
    }

    public void SaveMasteries() {
      RiotServices.MasteryBookService.SaveMasteryBook(Masteries);
    }

    public async Task<List<SummonerRune>> GetRuneInventory() {
      return (await RiotServices.SummonerRuneService.GetSummonerRuneInventory(SummonerID)).SummonerRunes;
    }

    #endregion

    public virtual async Task<ChampionDTO[]> GetAvailableChampions() {
      return await RiotServices.InventoryService.GetAvailableChampions();
    }

    public async Task<SummonerIconInventoryDTO> GetSummonerIconInventory() {
      return await RiotServices.SummonerIconService.GetSummonerIconInventory(SummonerID);
    }

    public void SetSummonerIcon(int id) {
      RiotServices.SummonerService.UpdateProfileIconId(id);
      LoginPacket.AllSummonerData.Summoner.ProfileIconId = id;
    }
  }
}
