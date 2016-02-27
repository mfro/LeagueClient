using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace RiotClient.Settings {
  [Serializable]
  [XmlRoot("dictionary")]
  public sealed class SerializableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, IXmlSerializable {
    #region IXmlSerializable Members
    System.Xml.Schema.XmlSchema IXmlSerializable.GetSchema() => null;

    void IXmlSerializable.ReadXml(XmlReader reader) {
      var keySerializer = new XmlSerializer(typeof(TKey));
      var valueSerializer = new XmlSerializer(typeof(TValue));

      bool wasEmpty = reader.IsEmptyElement;
      reader.Read();

      if (wasEmpty)
        return;

      while (reader.NodeType != XmlNodeType.EndElement) {
        reader.ReadStartElement("item");

        reader.ReadStartElement("key");
        TKey key = (TKey) keySerializer.Deserialize(reader);
        reader.ReadEndElement();

        reader.ReadStartElement("value");
        TValue value = (TValue) valueSerializer.Deserialize(reader);
        reader.ReadEndElement();

        this.Add(key, value);

        reader.ReadEndElement();
        reader.MoveToContent();
      }
      reader.ReadEndElement();
    }

    void IXmlSerializable.WriteXml(XmlWriter writer) {
      var keySerializer = new XmlSerializer(typeof(TKey));
      var valueSerializer = new XmlSerializer(typeof(TValue));

      foreach (TKey key in Keys) {
        writer.WriteStartElement("item");

        writer.WriteStartElement("key");
        keySerializer.Serialize(writer, key);
        writer.WriteEndElement();

        writer.WriteStartElement("value");
        TValue value = this[key];
        valueSerializer.Serialize(writer, value);
        writer.WriteEndElement();

        writer.WriteEndElement();
      }
    }
    #endregion
  }
}
