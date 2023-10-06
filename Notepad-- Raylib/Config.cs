using Raylib_CsLo;
using System.IO;
using System.Xml.Serialization;

namespace Notepad___Raylib {
   public class Config {
      [XmlElement] public int fontSize = 19;
      [XmlElement] public int leftPadding = 4;
      [XmlElement] public int spacingBetweenLines = 0;
      [XmlElement] public int tabSize = 4;
      [XmlElement] public string fontName = "Inconsolata-Medium.ttf";
      [XmlElement] public Color textColor = new Color(215, 215, 215, 255);
      [XmlElement] public Color backgroundColor = new Color(31, 31, 31, 50);
      [XmlElement] public Color cursorColor = new Color(227, 227, 227, 255);

      public void Deserialize(string path) {
         XmlSerializer serializer = new XmlSerializer(typeof(Config));

         using Stream reader = new FileStream(path, FileMode.Open);
         Config config = serializer.Deserialize(reader) as Config;

         fontSize = config.fontSize;
         leftPadding = config.leftPadding;
         spacingBetweenLines = config.spacingBetweenLines;
         tabSize = config.tabSize;
         fontName = config.fontName;
         textColor = config.textColor;
         backgroundColor = config.backgroundColor;
         cursorColor = config.cursorColor;
      }

      public void Serialize(string path) {
         XmlSerializer serializer = new XmlSerializer(typeof(Config));

         if(!File.Exists(path)) File.Create(path).Close();

         using Stream writer = new FileStream(path, FileMode.Truncate);
         serializer.Serialize(writer, this);
      }
   }
}
