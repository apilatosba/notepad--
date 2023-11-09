using Raylib_CsLo;
using System;
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
      [XmlElement] public Color selectionColor = new Color(61, 129, 207, 102);
      [XmlElement] public string backgroundImage = "482950.png";
      [XmlElement] public float backgroundLucidity = 0.5f;

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
         selectionColor = config.selectionColor;
         backgroundImage = config.backgroundImage;
         backgroundLucidity = config.backgroundLucidity;
      }

      public void Serialize(string path) {
         XmlSerializer serializer = new XmlSerializer(typeof(Config));

         if (!File.Exists(path)) File.Create(path).Close();

         for (int i = 0, threshold = 5; ;) {
            try {
               using Stream writer = new FileStream(path, FileMode.Truncate);
               serializer.Serialize(writer, this);
               break;
            }
            catch (IOException e) {
               i++;

               if(i > threshold) {
                  Console.WriteLine($"ERROR: Couldn't serialize. Tried {threshold} times. Exception message: {e.Message}");
                  break;
               }
            }
         }
      }
   }
}
