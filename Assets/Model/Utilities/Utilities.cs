using System.Xml;
using System.Collections.Generic;
using UnityEngine;

public class Utilities  {

    public static Color hexToColor(string hex) {
        Color c = new Color();
        ColorUtility.TryParseHtmlString(hex, out c);
        return c;
    }

    public class statsXMLreader {
        public static XmlDocument doc;

        public static string stats_fileName = System.IO.Directory.GetCurrentDirectory() + "/assets/xml_defs/stats.xml";

        public static string getParameterFromXML(string caller, string field = null) {
            if (doc == null) { // load the doc if its null
                doc = new XmlDocument();
                doc.Load(stats_fileName);
            }
            XmlNode node;
            if (field == null) {
                node = doc.DocumentElement.SelectSingleNode("/stats/" + caller + "[1]");
            } else {
                node = doc.DocumentElement.SelectSingleNode("/stats/" + caller + "/" + field + "[1]");
            }
            if (node != null) {
                return node.InnerText;
            } else
                return null;
        }

        public static string[] getParametersFromXML(string caller, string field = null) {
            List<string> strings;
            if (doc == null) { // load the doc if its null
                doc = new XmlDocument();
                doc.Load(stats_fileName);
            }
            XmlNodeList nodes;
            if (field == null) {
                nodes = doc.DocumentElement.SelectNodes("/stats/" + caller);
            } else {
                nodes = doc.DocumentElement.SelectNodes("/stats/" + caller + "/" + field);
            }
            if (nodes != null) {
                strings = new List<string>();
                foreach (XmlNode node in nodes)
                    strings.Add(node.InnerText);
            } else
                return null;
            return strings.ToArray();
        }
    }

    public static float[,] mergeArrays(float[,] a, float[,] b, float weightA, float weightB) {
        // works with arrays of different size
        bool choice = a.GetLength(0) > b.GetLength(0);
        float[,] c = (choice) ? new float[a.GetLength(0), a.GetLength(0)] : new float[b.GetLength(0), b.GetLength(0)];
        double ratio = (double)a.GetLength(0) / b.GetLength(0);
        for (int i = 0; i < c.GetLength(0); i++) {
            for (int j = 0; j < c.GetLength(0); j++) {
                // sum weighted values
                if (choice) {
                    c[i, j] = weightA * a[i, j] + weightB * b[(int)(i / ratio), (int)(j / ratio)];
                } else {
                    c[i, j] = weightA * a[(int)(i * ratio), (int)(j * ratio)] + weightB * b[i, j];
                }
                // rescale the values back
                c[i, j] /= (weightA + weightB);
            }
        }
        return c;
    }
}
