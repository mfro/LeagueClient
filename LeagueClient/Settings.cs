using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MFroehlich.Parsing.DynamicJSON;
using MFroehlich.Parsing;

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
        var setting = prop.GetCustomAttributes(typeof(SettingAttribute), true)[0];
        prop.SetValue(this, ((SettingAttribute) setting).DefaultValue);
      }
      settingsFile = settingsfile;
      if (File.Exists(settingsfile)) Load();
    }

    public void Save() {
      var props = from p in typeof(Settings).GetProperties()
                  where Attribute.IsDefined(p, typeof(SettingAttribute))
                  where p.GetValue(this) != ((SettingAttribute) p.GetCustomAttributes(typeof(SettingAttribute), true)[0]).DefaultValue
                  select p;
      using (var save = new FileStream(settingsFile, FileMode.Create)) {
        foreach (var prop in props) {
          var key = prop.Name;
          var type = GetType(prop.GetValue(this));
          save.WriteByte((byte) key.Length);
          save.WriteString(key);

          save.WriteByte((byte) type);
          switch (type) {
            case SettingType.String:
              var str = (string) prop.GetValue(this);
              save.WriteInt(str.Length);
              save.WriteString(str);
              break;
            case SettingType.True:
            case SettingType.False: break;
            case SettingType.Bytes:
              var bytes = (byte[]) prop.GetValue(this);
              save.WriteInt(bytes.Length);
              save.Write(bytes);
              break;
          }
        }
        save.WriteByte(0);
      }
      //var json = new JSONObject();
      //var props = from p in typeof(Settings).GetProperties()
      //            where Attribute.IsDefined(p, typeof(SettingAttribute))
      //            select p;
      //foreach (var prop in props) {
      //  var setting = prop.GetCustomAttributes(typeof(SettingAttribute), true)[0];
      //  if(prop.GetValue(this) != ((SettingAttribute) setting).DefaultValue)
      //    json.Add(prop.Name, prop.GetValue(this));
      //}
      //File.WriteAllBytes(settingsFile, MFroFormat.Serialize(json));
    }

    private void Load() {
      using (var load = new FileStream(settingsFile, FileMode.Open)) {
        int len;
        while ((len = load.ReadByte()) > 0) {
          var bytes = new byte[len];
          load.ReadFully(bytes, 0, bytes.Length);
          var key = Encoding.UTF8.GetString(bytes);
          var prop = typeof(Settings).GetProperty(key);
          if (prop == null) continue;
          var type = (SettingType) load.ReadByte();
          switch (type) {
            case SettingType.String:
              int length = load.ReadInt();
              bytes = new byte[length];
              load.ReadFully(bytes, 0, length);
              prop.SetValue(this, Encoding.UTF8.GetString(bytes));
              break;
            case SettingType.True: prop.SetValue(this, true); break;
            case SettingType.False: prop.SetValue(this, false); break;
            case SettingType.Bytes:
              length = load.ReadInt();
              bytes = new byte[length];
              load.ReadFully(bytes, 0, length);
              prop.SetValue(this, bytes);
              break;
          }
        }
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
