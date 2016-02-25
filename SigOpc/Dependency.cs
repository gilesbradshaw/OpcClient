using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

using System.Text;
using System.Threading.Tasks;
using OpcLabs.EasyOpc.UA;
using OpcLabs.EasyOpc.UA.OperationModel;
using OpcLabs.EasyOpc.UA.Reactive;


namespace SigOpc
{
    using System.Reactive.Concurrency;
    using System.Reactive.Linq;
    using static XmlUtilities;

    static class Dependency
    {
        public static async Task Do(IObservable<string> input)
        {
            var config = Config();

            using (config.Root
                .Elements("dependencies")
                .SelectMany(d=>d.Elements("dependency"))
                .Select(dependency => new {
                    to = dependency.Element("to"),
                    from = dependency.Element("from")})
                //combine the latest values from the from and to tags
                .Select(dependency =>
                    //keep re trying if the write fails
                    Observable.Retry(                   
                        Observable.CombineLatest<EasyUAMonitoredItemChangedEventArgs>(
                            new List<IObservable<EasyUAMonitoredItemChangedEventArgs>> {
                                ReadValues(dependency.from),
                                ReadValues(dependency.to)
                            })
                         //if both tags have been read ok..
                         .Where(results => results.First().Succeeded && results.Last().Succeeded)
                         // and there values are different
                         .Where(results => results.First().AttributeData.Value != results.Last().AttributeData.Value)
                         // select the from tags value
                         .Select(results => results.First().AttributeData.Value)
                         //and write to the to tag
                         .Select(WriteValue(dependency.to)))
                     .Subscribe()
            ).ToList().Disposer())
                await input.Take(1);
        }
        //subscibes to a tag defined by an XElement
        static UAMonitoredItemChangedObservable<object> ReadValues(XElement xElement)
        {
            return UAMonitoredItemChangedObservable.Create<object>(
                new EasyUAMonitoredItemArguments(
                    null,
                    GetElementAttribute(xElement, "ua", "endpoint"),
                    $"ns=2;s={xElement.Attribute("node").Value}",
                    int.Parse(GetAttribute(xElement, "updateRate"))));
        }
        //writes a value to a tag as defined by an XElement
        static Func<object, object> WriteValue(XElement xElement)
        {
            return value =>
            {
                using (var uaClient = new EasyUAClient())
                {
                    uaClient.WriteValue(
                        new UAWriteValueArguments(
                           GetElementAttribute(xElement, "ua", "endpoint"),
                           $"ns=2;s={xElement.Attribute("node").Value}",
                           value
                        )
                    );
                    return value;
                };
            };
        }
    }
}
