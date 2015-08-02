using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using MFroehlich.Parsing;
using MFroehlich.Parsing.DynamicJSON;
using static System.Reflection.CustomAttributeExtensions;

namespace LeagueClient {
  public class Settings {
    private string settingsFile;

    [Setting("")]
    public string Username { get; set; }
    [Setting(null)]
    public byte[] Password { get; set; }
    [Setting(false)]
    public bool AutoLogin { get; set; }
    [Setting(true)]
    public bool Animation { get; set; }

    public Settings(string settingsfile) {
      var props = from p in typeof(Settings).GetProperties()
                  where Attribute.IsDefined(p, typeof(SettingAttribute))
                  select p;
      foreach (var prop in props) {
        var setting = prop.GetCustomAttribute<SettingAttribute>();
        prop.SetValue(this, setting.DefaultValue);
      }
      this.settingsFile = settingsfile;
      if (File.Exists(settingsfile)) Load();
    }

    public void Save() {
      var props = from p in typeof(Settings).GetProperties()
                  where Attribute.IsDefined(p, typeof(SettingAttribute))
                  select p;
      var doc = new XmlDocument();
      doc.LoadXml("<Settings/>");

      foreach (var prop in props) {
        var node = doc.CreateElement("Setting");
        node.SetAttribute("Name", prop.Name);
        var value = prop.GetValue(this);
        if (value is bool || value is string) node.SetAttribute("Value", value.ToString());
        else if(value is byte[]) node.SetAttribute("Value", Convert.ToBase64String((byte[]) value));
        doc.DocumentElement.AppendChild(node);
      }

      doc.Save(settingsFile);
    }

    private void Load() {
      var doc = new XmlDocument();
      doc.Load(settingsFile);

      foreach(XmlElement node in doc.DocumentElement.ChildNodes) {
        var prop = GetType().GetProperty(node.Attributes["Name"].Value);
        if (prop.PropertyType == typeof(string)) prop.SetValue(this, node.Attributes["Value"].Value);
        else if (prop.PropertyType == typeof(bool)) prop.SetValue(this, bool.Parse(node.Attributes["Value"].Value));
        else if (prop.PropertyType == typeof(byte[])) prop.SetValue(this, Convert.FromBase64String(node.Attributes["Value"].Value));
      }
    }

    public static SettingType GetType(object value) {
      if (value is string) return SettingType.String;
      if (value is bool)
        if ((bool) value) return SettingType.True;
        else return SettingType.False;
      if (value is byte[]) return SettingType.Bytes;
      throw new ArgumentException(value + "");
    }

    public enum SettingType {
      String = 0,
      True = 1,
      False = 2,
      Bytes = 3
    }

    public class SettingAttribute : Attribute {
      public object DefaultValue { get; private set; }
      public SettingAttribute(object value) { DefaultValue = value; }
    }
  }
}
