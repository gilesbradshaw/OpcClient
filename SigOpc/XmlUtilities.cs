using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;


namespace SigOpc
{
    public static class XmlUtilities
    {
        public static string GetAttribute(XElement _this, string name)
        {
            if (_this != null)
                return _this.Attribute(name) != null
                    ? _this.Attribute(name).Value
                    : GetAttribute(_this.Parent, name);
            else
                return null;
        }
        public static string GetElementAttribute(XElement _this, string elementName, string name)
        {
            if (_this != null)
                return _this.Element(elementName) != null
                    ? (_this.Element(elementName).Attribute(name) != null ? _this.Element(elementName).Attribute(name).Value : GetElementAttribute(_this.Parent, elementName, name))
                    : GetElementAttribute(_this.Parent, elementName, name);
            else
                return null;
        }

        public static XDocument Config()
        {
            return XDocument.Load("dms.xml");
        }

        public static IEnumerable<Collection> Collections(XDocument config, string group)
        {
            return config.Root.Elements("group").Where(e => e.Attribute("name").Value == group)
                .SelectMany(e=> e.Elements("col").Select((XElement col, int index) => new Collection { Col = col, Index = index }));

        }
        public class Collection
        {
            public int Index { get; set; }
            public XElement Col { get; set; }
        }
    }
}
