using LeagueClient.Logic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Markup;

namespace LeagueClient.UI.Util {
  public class StringExtension : MarkupExtension {

    public StringExtension(string path) {
      Path = path;
    }

    [ConstructorArgument("path")]
    public string Path { get; set; }

    public override object ProvideValue(IServiceProvider serviceProvider) {
      try {
        var paths = Path.Split('.');
        var prop = LoLClient.Strings.GetType().GetProperty(paths[0]);
        object strings = prop.GetValue(LoLClient.Strings);

        prop = strings.GetType().GetProperty(paths[1]);
        var value = prop.GetValue(strings);
        return value;
      } catch (Exception x) {
        throw new ArgumentException("String not found: " + Path, x);
      }
    }
  }
}
