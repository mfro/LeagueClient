using LeagueClient.Logic;
using RiotClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
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
  /// Interaction logic for NewsItem.xaml
  /// </summary>
  public partial class NewsItem : UserControl {
    public string Title { get; }
    public string Link { get; }
    public string Description { get; }
    public Uri ImageUrl { get; }
    public DateTime Date { get; }

    public NewsItem(XmlElement xml) {
      InitializeComponent();

      var root = new Uri("http://" + new Uri(Session.Region.NewsURL).Host);
      foreach (XmlElement node in xml.ChildNodes) {
        switch (node.Name) {
          case "title":
            Title = node.InnerText;
            break;
          case "link":
            Link = node.InnerText;
            break;
          case "description":
            int start = node.InnerText.IndexOf(">") + 1;
            int end = node.InnerText.IndexOf("<", start);
            Description = HttpUtility.HtmlDecode(node.InnerText.Substring(start, end - start));
            start = node.InnerText.IndexOf("img");
            start = node.InnerText.IndexOf("src=\"", start) + 5;
            end = node.InnerText.IndexOf("?itok=");
            if (end < 0) end = node.InnerText.IndexOf('"', start);
            ImageUrl = new Uri(root, node.InnerText.Substring(start, end - start));
            break;
        }
      }

      NewsIcon.Source = new BitmapImage(ImageUrl); ;
      DescriptionText.Text = Description;
    }

    private void Border_MouseUp(object sender, MouseButtonEventArgs e) {
      System.Diagnostics.Process.Start(Link);
    }
  }
}
