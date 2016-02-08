using OpcLabs.BaseLib.ComInterop;
using OpcLabs.EasyOpc;
using OpcLabs.EasyOpc.DataAccess;
using OpcLabs.EasyOpc.DataAccess.OperationModel;
using OpcLabs.EasyOpc.DataAccess.Reactive;
using OpcLabs.EasyOpc.UA;
using OpcLabs.EasyOpc.UA.OperationModel;
using OpcLabs.EasyOpc.UA.Reactive;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reactive.Linq;
using System.Xml.Linq;
using System.IO;

namespace AlsSampleOpcClient
{
    class Program
    {
        //locks access to the console
        static readonly object consoleLock = new object();
        
        //writing a value
        static void Writing(object value)
        {
            using(new ConsoleColourer(ConsoleColor.Green, ConsoleColor.Black))
                Console.WriteLine(string.Format("writing {0}", value));
        }

        //Read a value
        static void Read(object value)
        {
            using (new ConsoleColourer(ConsoleColor.Magenta, ConsoleColor.Black))
                Console.WriteLine(string.Format("READ: {0}", value));
        }

        //displays a tag value at a position
        static void PosDisplay(XElement config, object value, bool quality)
        {
            lock (consoleLock)
            {
                using (new ConsoleColourer(quality ? ConsoleColor.Green : ConsoleColor.Yellow, ConsoleColor.Black))
                {
                    TitleDisplay(config);
                    var line = int.Parse(config.Attribute("line").Value);
                    var startCol = int.Parse(config.Attribute("col").Value) * 40;

                    using (new ConsolePositioner(startCol + 15, line, 10))
                    {
                        Console.Write(value);
                    }
                }
            }
        }

        //displays a tag title
        static XElement TitleDisplay(XElement config)
        {
            lock (consoleLock)
            {
                var line = int.Parse(config.Attribute("line").Value);
                var startCol = int.Parse(config.Attribute("col").Value) * 40;

                using (new ConsolePositioner( startCol, line, 40))
                {
                    Console.Write(config.Attribute("id").Value);
                    Console.SetCursorPosition(startCol + 5, line);
                    Console.Write(config.Attribute("name").Value);
                }
                
                return config;
            }
        }

        //displays a tag exception
        static void ExceptionDisplay(XElement config, Exception ex)
        {

            lock (consoleLock)
            {
                using (new ConsoleColourer(ConsoleColor.Red, ConsoleColor.Black))
                {
                    TitleDisplay(config);

                    var line = int.Parse(config.Attribute("line").Value);
                    var startCol = int.Parse(config.Attribute("col").Value) * 40;

                    using (new ConsolePositioner(startCol + 15, line, 24))
                    {
                        Console.Write(ex.GetType().Name);
                    }
                }
            }
        }


        
        static void Main(string[] args)
        {
           Console.BackgroundColor = ConsoleColor.Black;
           Console.ForegroundColor = ConsoleColor.Gray;

           //pick an option based on command line
           try {
                switch(args[0].ToLower()){
                        case "d":
                            Da();
                            break;
                        case "u":
                            Ua();
                            break;
                        case "ux":
                            UaRx(args[1]);
                            break;
                        case "dx":
                            DaRx(args[1]);
                            break;
                        case "uxsim":
                            UaRxSim();
                            break;
                        case "ulog":
                            UaLog(args[1], args[2]);
                            break;
                        default:
                            break;
                    }
                }
                catch (Exception ex)
                {
                    using (new ConsoleColourer(ConsoleColor.Black, ConsoleColor.Yellow))
                        Console.WriteLine(string.Format("Exception: {0}", ex.Message));
                    
                }

            
            System.Threading.Thread.Sleep(2000);

        }


        // simple ua client with hard coded config
        static void Ua()
        {
            Console.Clear();
            using (new ConsoleColourer(ConsoleColor.Yellow, ConsoleColor.Black))
                Console.WriteLine("UA! Enter any number to write a value return to exit");
            

            var uaClient = new EasyUAClient();

            uaClient.MonitoredItemChanged += (sender, e) =>
                {
                    lock(consoleLock)
                        using (new ConsolePositioner(0, 5, Console.BufferWidth))
                            Console.Write(
                                 string.Format("{0} {1} {2}",
                                     e.Arguments.State,
                                     e.AttributeData.StatusCode,
                                     e.AttributeData.Value
                                 )
                            );
                };


           var uASubscribeArguments = new EasyUAMonitoredItemArguments(
                 "uaState",
                 "opc.tcp://127.0.0.1:49320/",
                 "ns=2;s=Channel1.Device1.Tag1",
                 1000);

           uaClient.SubscribeMonitoredItem(uASubscribeArguments);

            var rxUaSubscription = UAMonitoredItemChangedObservable.Create<int>(uASubscribeArguments)
                .Subscribe(val =>
                    {
                        lock (consoleLock)
                            using (new ConsolePositioner(0, 4, Console.BufferWidth))
                                Console.WriteLine(
                                     string.Format("Rx ua observed {0} {1} {2}",
                                         val.Arguments.State,
                                         val.AttributeData.StatusCode,
                                         val.AttributeData.Value
                                     )
                                );
                    }
                );


            UAAttributeData attributeData = uaClient.Read(
                "opc.tcp://127.0.0.1:49320/",
                "ns=2;s=Channel1.Device1.Tag1");

            lock (consoleLock)
                using (new ConsolePositioner(0, 3, Console.BufferWidth))
                     Read(attributeData.DisplayValue());

            new ConsolePositioner(0, 7, Console.BufferWidth);
            var key = Console.ReadLine();
            
            while (key!="")
            {
                lock (consoleLock)
                    using (new ConsolePositioner(0, 6, Console.BufferWidth))
                        Writing(key);
                uaClient.WriteValue(
                    new UAWriteValueArguments(
                        "opc.tcp://127.0.0.1:49320/",
                       "ns=2;s=Channel1.Device1.Tag1",
                        key
                    )
                );
                new ConsolePositioner(0, 7, Console.BufferWidth);
                key = Console.ReadLine();
            }
            rxUaSubscription.Dispose();
            uaClient.UnsubscribeAllMonitoredItems();
            uaClient.Dispose();
        }

        // simple da client with hard coded config
        static void Da()
        {
            Console.Clear();
            using (new ConsoleColourer(ConsoleColor.Yellow, ConsoleColor.Black))
                Console.WriteLine("DA! Enter any number to write a value or return to exit");
            var daClient = new OpcLabs.EasyOpc.DataAccess.EasyDAClient();

            daClient.ItemChanged += (sender, e) => {
                lock (consoleLock)
                    using (new ConsolePositioner(0, 2, Console.BufferWidth))
                    {
                        Console.WriteLine(string.Format("da {0} {1} {2}",
                            e.Arguments.State,
                            e.Vtq.Quality,
                            e.Vtq.Value)
                        );
                    }
            };
                

            daClient.SubscribeItem(
                "localhost",
                "Kepware.KEPServerEX.V5",
                "Channel1.Device1.Tag1",
                VarTypes.Int,
                1000,
                "daState"
            );

            var rxDaSubscription = DAItemChangedObservable.Create<int>(
                "localhost",
                "Kepware.KEPServerEX.V5",
                "Channel1.Device1.Tag1",
                1000).Subscribe(val => 
                    {
                        lock (consoleLock)
                            using (new ConsolePositioner(0, 3, Console.BufferWidth))
                            {
                                Console.Write(string.Format("Rx da observed {0} {1}", val.Vtq.DisplayValue(), val.Vtq.Quality));
                            }
                     }
                );


            var item = daClient.ReadItem(
                new ServerDescriptor("localhost", "Kepware.KEPServerEX.V5"),
                new DAItemDescriptor("Channel1.Device1.Tag1")
            );
            lock(consoleLock)
                using (new ConsolePositioner(0,1, Console.BufferWidth))
                {
                    Read(item.DisplayValue());
                }


            new ConsolePositioner(0, 7, Console.BufferWidth);
            var key = Console.ReadLine();

            while (key !="")
            {
                lock(consoleLock)
                    using (new ConsolePositioner(0, 6, Console.BufferWidth))
                    {
                        Writing(key);
                    }
                Console.SetCursorPosition(0, 7);
                Console.Write(new string(' ', Console.BufferWidth));
                Console.SetCursorPosition(0, 7);
                daClient.WriteItemValue(
                    new ServerDescriptor("localhost", "Kepware.KEPServerEX.V5"),
                    new DAItemDescriptor("Channel1.Device1.Tag1"),
                    key
                );
                new ConsolePositioner(0, 7, Console.BufferWidth);
                key = Console.ReadLine();
            }

            rxDaSubscription.Dispose();
            daClient.UnsubscribeAllItems();
            daClient.Dispose();

        }

        // configured da client using rx
        static void DaRx(string fileName)
        {

            Console.Clear();
            using (new ConsoleColourer(ConsoleColor.Yellow, ConsoleColor.Black))
                Console.WriteLine("reactive DA! Enter id <space> value to write a value,  Return to quit");
            

            var daClient = new EasyDAClient();

            new ConsolePositioner(0, 10, Console.BufferWidth);

            var config = XDocument.Load(fileName);
            var subs = config.Root
                .Elements("tags")
                .SelectMany(tags => tags.Elements("tag"))
                .Select(tag => TitleDisplay(tag))
                .Select(
                    tag =>
                    {
                        var daConfig = tag.Parent.Element("da");
                        IObservable<EasyDAItemChangedEventArgs> ret = null;
                        var args = new DAItemGroupArguments(
                            daConfig.Attribute("endpoint").Value,
                            daConfig.Attribute("server").Value,
                            daConfig.Attribute("nodePrefix").Value + tag.Attribute("node").Value,
                            int.Parse(tag.Attribute("updateInterval").Value), 
                            tag.Attribute("id").Value);
                        switch (tag.Attribute("type").Value)
                        {
                            case "int":
                                ret = DAItemChangedObservable.Create<int>(args);
                                break;
                            case "bool":
                                ret = DAItemChangedObservable.Create<bool>(args);
                                break;
                            case "double":
                                ret = DAItemChangedObservable.Create<double>(args);
                                break;
                            case "float":
                                ret = DAItemChangedObservable.Create<float>(args);
                                break;
                            default:
                                ret = DAItemChangedObservable.Create<string>(args);
                                break;
                        }

                        return ret
                            .Subscribe(
                                val => PosDisplay(tag, val.Vtq.Value, val.Vtq.Quality.ToString().StartsWith("Good")),
                                (ex) => ExceptionDisplay(tag, ex),
                                () => { }
                            );
                    }
                ).ToList();

            
            var key = Console.ReadLine();

            while (key != "")
            {
                lock (consoleLock)
                {
                    var vals = key.Split(' ');
                    try
                    {
                        if (vals.Length != 2)
                        {
                            throw new Exception("enter id [space] val");
                        }
                        var tag = config.Root
                            .Elements("tags")
                            .SelectMany(tags => tags.Elements("tag"))
                            .Single(t => t.Attribute("id").Value == vals[0]);
                        var daConfig = tag.Parent.Element("da");
                        daClient.WriteItemValue(
                            new ServerDescriptor(daConfig.Attribute("endpoint").Value, daConfig.Attribute("server").Value),
                            new DAItemDescriptor(daConfig.Attribute("nodePrefix").Value + tag.Attribute("node").Value),
                            vals[1]
                        );
                    }
                    catch (Exception ex)
                    {
                        lock (consoleLock)
                            using (new ConsolePositioner(0, 21, Console.BufferWidth))
                                Console.WriteLine(ex.GetType().Name);
                    }
                    Console.WindowTop = 0;
                }
                new ConsolePositioner(0, 10, Console.BufferWidth);
                key = Console.ReadLine();
            }
            Console.Clear();
            subs.ForEach(s => s.Dispose());
            daClient.Dispose();
        }



        // configured ua client using rx
        static void UaRx(string fileName)
        {
            
            Console.Clear();
            using (new ConsoleColourer(ConsoleColor.Yellow, ConsoleColor.Black))
                Console.WriteLine("reactive UA! Enter id <space> value to write a value,  Return to quit");
            

            var uaClient = new EasyUAClient();

            new ConsolePositioner(0, 10, Console.BufferWidth);

            var config = XDocument.Load(fileName);

            var myTags = config.Root
                .Elements("tags")
                .SelectMany(tags=> tags.Elements("tag"));


            myTags
                .ToList()
                .ForEach(tag=>TitleDisplay(tag));

            var subs = myTags
                .Select(
                    tag => ItemValue.UaObservable(
                        tag.Attribute("id").Value,
                        tag.Parent.Element("ua").Attribute("endpoint").Value,
                        tag.Parent.Element("ua").Attribute("nodePrefix").Value + tag.Attribute("node").Value,
                        int.Parse(tag.Attribute("updateInterval").Value),
                        tag.Attribute("type").Value
                    )
                        .Subscribe(
                            val => PosDisplay(tag,val.Value, val.Good),
                            (ex) => ExceptionDisplay(tag, ex), 
                            ()=>{}
                        )
                ).ToList();

            
            var key = Console.ReadLine();

            while (key!="")
            {
                lock (consoleLock)
                {
                    var vals = key.Split(' ');
                    try {
                        if (vals.Length != 2)
                        {
                            throw new Exception("enter id [space] val");
                        } 
                        var tag = myTags
                            .Single(t=>t.Attribute("id").Value==vals[0]);
                        var uaConfig = tag.Parent.Element("ua");
                        uaClient.WriteValue(
                            new UAWriteValueArguments(
                               uaConfig.Attribute("endpoint").Value,
                               uaConfig.Attribute("nodePrefix").Value + tag.Attribute("node").Value,
                                vals[1]
                            )
                        );
                        
                    
                    } catch (Exception ex)
                    {
                        lock(consoleLock)
                            using( new ConsolePositioner(0, 21, Console.BufferWidth))
                                Console.WriteLine(ex.GetType().Name);
                    }
                    new ConsolePositioner(0, 10, Console.BufferWidth);
                    Console.WindowTop=0;
                    }
                key = Console.ReadLine();
            }
            Console.Clear();
            subs.ForEach(s=>s.Dispose());
            uaClient.Dispose();
        }

        // configured ua logging client using rx
        static void UaLog(string fileName, string logFileName)
        {

            var locker= new Object();
           var titleRow = "id, endpoint,name, tag, value, good, time";
            if(!File.Exists(logFileName))
            {
                using (System.IO.StreamWriter file = new System.IO.StreamWriter(logFileName, true))
                {
                    file.WriteLine(titleRow);
                }
            }

            var config = XDocument.Load(fileName);

            var myTags = config.Root
                .Elements("tags")
                .SelectMany(tags => tags.Elements("tag"));


            
            var subs = myTags
                .Select(
                    tag => ItemValue.UaObservable(
                        tag.Attribute("id").Value,
                        tag.Parent.Element("ua").Attribute("endpoint").Value,
                        tag.Parent.Element("ua").Attribute("nodePrefix").Value + tag.Attribute("node").Value,
                        int.Parse(tag.Attribute("updateInterval").Value),
                        tag.Attribute("type").Value
                    )
                        .Subscribe(
                            val => {
                                var uaConfig = tag.Parent.Element("ua");
                                lock(locker)
                                    using (System.IO.StreamWriter file = new System.IO.StreamWriter(logFileName, true))
                                    {
                                        var log = string.Format("{0}, {1}, {2}, {3}, {4}, {5}, {6}", tag.Attribute("id").Value, uaConfig.Attribute("endpoint").Value, tag.Attribute("name").Value, uaConfig.Attribute("nodePrefix").Value + tag.Attribute("node").Value, val.Value, val.Good, DateTime.Now.ToLongTimeString());
                                        file.WriteLine(log);
                                        Console.WriteLine(log);
                                    }

                                
                            }, 
                            (ex) => ExceptionDisplay(tag, ex),
                            () => { }
                        )
                ).ToList();


            var key = Console.ReadLine();
            subs.ForEach(s => s.Dispose());

        }



        // ua simulator hard coded + hard coded config
        static void UaRxSim()
        {

            Console.Clear();
            using (new ConsoleColourer(ConsoleColor.Yellow, ConsoleColor.Black))
                Console.WriteLine("reactive UA simulation! Press a key to quit.");
            

            var uaClient = new EasyUAClient();

            var rxUaSubscription = UAMonitoredItemChangedObservable.Create<int>(new EasyUAMonitoredItemArguments(
               "uaState",
               "opc.tcp://127.0.0.1:49320/",
               "ns=2;s=Channel1.Device1.Tag2",
               0)
             )
             .Subscribe(val =>
             {
                 Console.WriteLine(string.Format("wrote {0} to {1} on {2} being {3}", val.AttributeData.Value, "ns=2;s=Channel1.Device1.Tag1", "ns=2;s=Channel1.Device1.Tag2", val.AttributeData.Value));
                 uaClient.WriteValue(
                     new UAWriteValueArguments(
                         "opc.tcp://127.0.0.1:49320/",
                         "ns=2;s=Channel1.Device1.Tag1",
                         val.AttributeData.Value
                     )
                 );

             }
         );

            var key = Console.ReadKey();

            rxUaSubscription.Dispose();
            uaClient.Dispose();
        }

    

    }


    //static method to create an IObservable of itself which gets ua disconnects
    public class ItemValue
    {
        public object Value { get ; set;}
        public bool Good { get;set;}

        static public IObservable<ItemValue> UaObservable(object status, string endpoint, string node, int updateInterval, string type)
        {
            var args = new EasyUAMonitoredItemArguments(
                status,
                endpoint,
                node,
                updateInterval
            );
            var uaClient = new EasyUAClient();


            var check =  (Observable.Return<long>(0).Concat(Observable.Interval(
                TimeSpan.FromSeconds(60))).Select(t =>
                    {
                        try
                        {
                            return uaClient.Read(
                                endpoint,
                                node
                            ).HasGoodStatus;
                        }
                        catch (Exception)
                        {
                            return false;
                        } 
                    }
                ).Distinct());


            return check.SelectMany(c =>
            {
                if (!c)
                {
                    return Observable.Return(new ItemValue{Good=false});
                }
                else
                {
                    IObservable<EasyUAMonitoredItemChangedEventArgs> ret = null;
                    switch (type)
                    {
                        case "int":
                            ret = UAMonitoredItemChangedObservable.Create<int>(args);
                            break;
                        case "bool":
                            ret = UAMonitoredItemChangedObservable.Create<bool>(args);
                            break;
                        case "double":
                            ret = UAMonitoredItemChangedObservable.Create<double>(args);
                            break;
                        case "float":
                            ret = UAMonitoredItemChangedObservable.Create<Single>(args);
                            break;
                        default:
                            ret = UAMonitoredItemChangedObservable.Create<string>(args);
                            break;
                    }
                    return ret.Select(
                        r => new ItemValue
                        {
                            Value = r.AttributeData != null ? r.AttributeData.Value : null,
                            Good = r.AttributeData != null && r.AttributeData.StatusCode.ToString().StartsWith("Good")
                        }
                    );
                }
            });
            

            
        } 
    }

    public class ConsolePositioner : IDisposable
    {
        int _top, _left;
        public ConsolePositioner(int left, int top, int blank)
        {
            _top= Console.CursorTop;
            _left = Console.CursorLeft;
            Console.SetCursorPosition(left,top);
            Console.Write(new string(' ', blank));
            Console.SetCursorPosition(left, top);
        }

        public void Dispose()
        {
            Console.SetCursorPosition(_left, _top);
        }
    }



    public class ConsoleColourer : IDisposable
    {
        ConsoleColor _foreGround, _backGround;
        public ConsoleColourer( ConsoleColor foreground, ConsoleColor backGround)
        {
            _foreGround = Console.ForegroundColor;
            _backGround = Console.BackgroundColor;
            Console.ForegroundColor= foreground;
            Console.BackgroundColor = backGround;
        }

        public void Dispose()
        {
            Console.ForegroundColor= _foreGround;
            Console.BackgroundColor = _backGround;
        }
    }
}
