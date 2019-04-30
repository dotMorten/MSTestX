using AndroidXml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml.Linq;

namespace MSTestX.Console
{
    internal static class ApkHelper
    {
        public static void GetAPKInfo(string path, out string apk_id, out string activity)
        {
            apk_id = "";
            activity = "";
            using (MemoryStream ms = new MemoryStream())
            {
                using (var file = System.IO.Compression.ZipFile.OpenRead(path))
                {
                    var entry = file.GetEntry("AndroidManifest.xml");
                    if (entry != null)
                    {
                        using (var manifestStream = entry.Open())
                        {
                            manifestStream.CopyTo(ms);
                            ms.Seek(0, SeekOrigin.Begin);
                        }
                    }
                }
                var reader = new AndroidXmlReader(ms);
                while (reader.Read())
                {
                    if (reader.NodeType == System.Xml.XmlNodeType.Element && reader.Name == "manifest")
                    {
                        if (reader.MoveToAttribute("package"))
                        {
                            apk_id = reader.Value;
                        }
                        
                    }
                    else if(reader.NodeType == System.Xml.XmlNodeType.Element && reader.Name == "activity")
                    {
                        if (reader.MoveToAttribute("name", "http://schemas.android.com/apk/res/android"))
                        {
                            activity = reader.Value;
                        }
                    }
                }
            }
        }
    }
}
