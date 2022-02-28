using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace ScoreUpdater
{
    public class Utils
    {
        public static string XmlSerializeUtf8(object o)
        {
            string result;

            var ns = new XmlSerializerNamespaces();
            ns.Add(string.Empty, string.Empty);

            var serializer = new XmlSerializer(o.GetType());

            using (var stream = new MemoryStream())
            {
                var xmlTextWriter = XmlWriter.Create(stream, new XmlWriterSettings { Indent = true, Encoding = Encoding.UTF8 });
                serializer.Serialize(xmlTextWriter, o, ns);
                xmlTextWriter.Flush();
                stream.Seek(0, SeekOrigin.Begin);
                using (var reader = new StreamReader(stream, Encoding.UTF8))
                {
                    result = reader.ReadToEnd();
                }
            }

            return result;
        }

        public static T? XmlDeserialize<T>(string xml) where T : class
        {
            var xmlSerializer = new XmlSerializer(typeof(T));

            using var stringReader = new StringReader(xml);

            var result = xmlSerializer.Deserialize(stringReader);

            return (T?)result;
        }

        public static T? XmlDeserializeFile<T>(string filePath) where T : class
        {
            string xml = File.ReadAllText(filePath);
            var result = XmlDeserialize<T>(xml);

            return result;
        }
    }
}