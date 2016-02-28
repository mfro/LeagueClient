using LeagueClient.Logic;
using RiotClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml;

namespace LeagueClient.UI.Client.Home {
  /// <summary>
  /// Interaction logic for HomePage.xaml
  /// </summary>
  public partial class HomePage : UserControl {
    public HomePage() {
      InitializeComponent();

      FetchNews();
    }

    private async void FetchNews() {
      Session.WebClient.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/48.0.2564.116 Safari/537.36");

      var data = await Session.WebClient.DownloadStringTaskAsync(Session.Region.NewsURL);
      XmlDocument xml = new XmlDocument();
      xml.LoadXml(data);

      var nodes = new List<NewsItem>();
      foreach (XmlElement node in xml.SelectNodes("/rss/channel/item")) {
        var item = new NewsItem(node);
        nodes.Add(item);
      }
      NewsList.ItemsSource = nodes;
    }
  }
}
