using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Markup;

namespace LeagueClient.ClientUI.Controls {
  public class MyExtension : MarkupExtension {
    public string Field { get; set; }

    public MyExtension(string name) {
      Field = name;
    }

    public override object ProvideValue(IServiceProvider serviceProvider) {
      var prop = typeof(App).GetProperty(Field);
      if (prop == null) throw new ArgumentException($"Property {Field} not found");
      return prop.GetValue(null);
    }
  }
}
