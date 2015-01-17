using System.Xml;

namespace TagJam18
{
    public class XmlNodeSimple
    {
        XmlNode xml;

        public XmlNodeSimple(XmlNode xml)
        {
            this.xml = xml;
        }

        public XmlNodeSimple this[string xpath]
        {
            get
            {
                return xml.SelectSingleNode(xpath);
            }
        }

        public XmlNodeList SelectNodes(string xpath)
        {
            return xml.SelectNodes(xpath);
        }

        public static implicit operator XmlNodeSimple(XmlNode xml)
        {
            return new XmlNodeSimple(xml);
        }

        public string Value
        {
            get { return xml.Value; }
        }

        public static implicit operator XmlNode(XmlNodeSimple xml)
        {
            return xml.xml;
        }
    }
}
