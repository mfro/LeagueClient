using RtmpSharp.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RiotClient.Riot.Platform {
  public class BaseSummoner {
    [SerializedName("internalName")]
    public String InternalName { get; set; }

    [SerializedName("acctId")]
    public Int64 AccountId { get; set; }

    [SerializedName("name")]
    public String Name { get; set; }

    [SerializedName("profileIconId")]
    public Int32 ProfileIconId { get; set; }

    [SerializedName("summonerId")]
    public Int64 SummonerId { get; set; }
  }
}
