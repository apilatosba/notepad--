using System.IO;
using System.Xml.Serialization;

namespace Notepad___Raylib {
   public class WindowSaveData {
      [XmlElement] public Int2 position;
      [XmlElement] public Int2 size = new Int2(1150, 560);
      [XmlElement] public bool maximized = false;

      public static WindowSaveData Deserialize(string path) {
         XmlSerializer serializer = new XmlSerializer(typeof(WindowSaveData));

         using Stream reader = new FileStream(path, FileMode.Open);
         WindowSaveData windowSaveData = serializer.Deserialize(reader) as WindowSaveData;

         return windowSaveData;
      }

      public void Serialize(string path) {
         XmlSerializer serializer = new XmlSerializer(typeof(WindowSaveData));

         if (!File.Exists(path)) File.Create(path).Close();

         using Stream writer = new FileStream(path, FileMode.Truncate);
         serializer.Serialize(writer, this);
      }
   }
}
