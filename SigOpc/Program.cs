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
using static System.Console;
using static System.ConsoleColor;
namespace SigOpc
{
    class Program
    {
        //locks access to the console
        static readonly object consoleLock = new object();
        
        //writing a value
        static void Writing(object value)
        {
            using(new ConsoleColourer(Green, Black))
                WriteLine($"writing {value}");
        }

        //Read a value
        static void Read(object value)
        {
            using (new ConsoleColourer(Magenta, Black))
                WriteLine($"READ: {value}");
        }
        static string GetAttribute(XElement _this, string name)
        {
            if(_this != null)
                return _this.Attribute(name) !=null
                    ? _this.Attribute(name).Value 
                    : GetAttribute(_this.Parent, name ) ;
            else
                return null;
        }

        //displays a tag value at a position
        static void PosDisplay(string id, int line, int column, XElement config, object value, bool quality)
        {
            if(!quality)
            {
                System.Diagnostics.Debug.WriteLine("bad");
            }
            lock (consoleLock)
            {
                using (new ConsoleColourer(quality ? Green : Yellow, Black))
                {
                    IdDisplay(id, line, column);
                }
                var startCol = column * 80;

                using (new ConsolePositioner(startCol + 65, line + 5, 10))
                {
                    Write(value);
                }
                
            }
        }

        //displays a tag title
        static void IdDisplay(string id, int line, int column)
        {

            lock (consoleLock)
            {
                var startCol = column * 80;

                using (new ConsolePositioner(startCol, line + 5, 0))
                {
                    Write(id);
                    
                }

            }
        }



        //displays a tag title
        static XElement TitleDisplay(int line, int column, XElement config)
        {
            
            lock (consoleLock)
            {
                var startCol = column * 80;

                using (new ConsolePositioner( startCol + 5, line + 5, 0))
                {
                    Write(config.Attribute("name").Value);
                }
                
                return config;
            }
        }

        //displays a tag title
        static XElement TagsDisplay(int line, int column, XElement config)
        {

            lock (consoleLock)
            {
                var startCol = column * 80;

                using (new ConsolePositioner(startCol, line + 5, 0))
                {
                    Write(config.Attribute("name").Value);
                }

                return config;
            }
        }



        //displays a tag exception
        static void ExceptionDisplay(string id, int line, int column, XElement config, Exception ex)
        {

            lock (consoleLock)
            {
                using (new ConsoleColourer(Red, Black))
                {
                    var startCol = column * 80;

                    using (new ConsolePositioner(startCol + 15, line + 5, 24))
                    {
                        Write(ex.GetType().Name);
                    }
                }
            }
        }



        static void Main(string[] args)
        {
            ConsoleColor fore = ForegroundColor, back = BackgroundColor;
            CancelKeyPress += (s, e) =>
            {
                ForegroundColor = fore; BackgroundColor = back;
                Clear();
            };
            MainAsync(args).Wait();
        }
        static async Task MainAsync(string[] args) { 

           BackgroundColor = Black;
           ForegroundColor = Gray;

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
                            await UaRx(args[1]);
                            break;
                        case "dx":
                            await DaRx(args[1]);
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
                    using (new ConsoleColourer(Black, Yellow))
                        WriteLine($"Exception: {ex.Message}");
                    
                }

            
            System.Threading.Thread.Sleep(2000);

        }


        // simple ua client with hard coded config
        static void Ua()
        {
            Clear();
            
            using (new ConsoleColourer(Yellow, Black))
                WriteLine("UA! Enter any number to write a value return to exit");

            new ConsolePositioner(0, 9, BufferWidth);
            var uaClient = new EasyUAClient();

            uaClient.MonitoredItemChanged += (sender, val) =>
                {
                    lock (consoleLock)
                        using (new ConsolePositioner(0, 6, BufferWidth*2))
                            using (new ConsoleColourer(val.Succeeded ? Gray : Yellow, Black))
                                WriteLine(
                                      string.Format("ua observed {0} {1} {2}",
                                          val.Arguments.State,
                                          val.Succeeded ? val.AttributeData.StatusCode.ToString() : val.ErrorMessageBrief,
                                          val.Succeeded ? val.AttributeData.Value : ""
                                      )
                                 );
                            
                        
                };


           var uASubscribeArguments = new EasyUAMonitoredItemArguments(
                 "uaState",
                 "opc.tcp://127.0.0.1:49320/",
                 "ns=2;s=Zone16.HSH.Finished",
                 1000);

           uaClient.SubscribeMonitoredItem(uASubscribeArguments);

            var rxUaSubscription = UAMonitoredItemChangedObservable.Create<int>(uASubscribeArguments)
                .Subscribe(val =>
                    {
                        lock (consoleLock)
                            using (new ConsolePositioner(0, 4, BufferWidth * 2))
                                using (new ConsoleColourer(val.Succeeded ? Gray : Yellow, Black))
                                   WriteLine(
                                         string.Format("Rx ua observed {0} {1} {2}",
                                             val.Arguments.State,
                                             val.Succeeded ? val.AttributeData.StatusCode.ToString() : val.ErrorMessageBrief,
                                             val.Succeeded ? val.AttributeData.Value : ""
                                         )
                                    );
                            
                                
                                
                    }
                );

            try
            {
                UAAttributeData attributeData = uaClient.Read(
                "opc.tcp://127.0.0.1:49320/",
                "ns=2;s=Zone16.HSH.Finished");

                lock (consoleLock)
                    using (new ConsolePositioner(0, 3, BufferWidth))
                        Read(attributeData.DisplayValue());

            }
            catch (Exception ex)
            {
                lock (consoleLock)
                    using (new ConsolePositioner(0, 3, BufferWidth))
                        using(new ConsoleColourer(Black, Yellow))
                            Write($"No read: {ex.GetType().Name}");
            }
            
            lock(consoleLock)
                new ConsolePositioner(0, 9, BufferWidth);

            var key = ReadLine();
            
            while (key!="")
            {
                lock (consoleLock)
                    using (new ConsolePositioner(0, 8, BufferWidth * 5))
                        Writing(key);
                try {
                    uaClient.WriteValue(
                        new UAWriteValueArguments(
                            "opc.tcp://127.0.0.1:49320/",
                           "ns=2;s=Zone16.HSH.Finished",
                            key
                        )
                    );
                }
                catch (Exception ex)
                {
                    lock (consoleLock)
                        using (new ConsolePositioner(0, 10, BufferWidth * 3))
                            using(new ConsoleColourer(Black, Yellow))
                                Write(ex.Message);
                }
                lock (consoleLock)
                    new ConsolePositioner(0, 9, BufferWidth);
                key = ReadLine();
            }
            rxUaSubscription.Dispose();
            uaClient.UnsubscribeAllMonitoredItems();
            uaClient.Dispose();
        }

        // simple da client with hard coded config
        static void Da()
        {
            Clear();
            using (new ConsoleColourer(Yellow, Black))
                WriteLine("DA! Enter any number to write a value or return to exit");
            var daClient = new OpcLabs.EasyOpc.DataAccess.EasyDAClient();

            daClient.ItemChanged += (sender, e) => {
                lock (consoleLock)
                    using (new ConsolePositioner(0, 2, BufferWidth))
                    {
                        WriteLine($"da {e.Arguments.State} {e.Vtq.Quality} {e.Vtq.Value}");
                    }
            };
                

            daClient.SubscribeItem(
                "localhost",
                "Kepware.KEPServerEX.V5",
                "Zone16.HSH.Finished",
                VarTypes.Bool,
                1000,
                "daState"
            );

            var rxDaSubscription = DAItemChangedObservable.Create<int>(
                "localhost",
                "Kepware.KEPServerEX.V5",
                "Zone16.HSH.Finished",
                1000).Subscribe(val => 
                    {
                        lock (consoleLock)
                            using (new ConsolePositioner(0, 3, BufferWidth))
                            {
                                Write($"Rx da observed {val.Vtq.DisplayValue()} {val.Vtq.Quality}");
                            }
                     }
                );


            var item = daClient.ReadItem(
                new ServerDescriptor("localhost", "Kepware.KEPServerEX.V5"),
                new DAItemDescriptor("Zone16.HSH.Finished")
            );
            lock(consoleLock)
                using (new ConsolePositioner(0,1, BufferWidth))
                {
                    Read(item.DisplayValue());
                }


            new ConsolePositioner(0, 7, BufferWidth);
            var key = ReadLine();

            while (key !="")
            {
                lock(consoleLock)
                    using (new ConsolePositioner(0, 6, BufferWidth))
                    {
                        Writing(key);
                    }
                SetCursorPosition(0, 7);
                Write(new string(' ', BufferWidth));
                SetCursorPosition(0, 7);
                daClient.WriteItemValue(
                    new ServerDescriptor("localhost", "Kepware.KEPServerEX.V5"),
                    new DAItemDescriptor("Zone16.HSH.Finished"),
                    key
                );
                new ConsolePositioner(0, 7, BufferWidth);
                key = ReadLine();
            }

            rxDaSubscription.Dispose();
            daClient.UnsubscribeAllItems();
            daClient.Dispose();

        }

        // configured da client using rx
        static async Task DaRx(string group)
        {

            await Rx(group,
                (tag) => (value) =>
                {
                    
                    using (var client = new EasyDAClient())
                    {

                        //client.InstanceParameters.UpdateRates.ReadAutomatic = int.Parse(GetAttribute(tag, "updateRate"));
                        //client.InstanceParameters.UpdateRates.WriteAutomatic = int.Parse(GetAttribute(tag, "updateRate"));

                        client.WriteItemValue(
                            tag.Parent.Element("da").Attribute("endpoint").Value,
                            tag.Parent.Element("da").Attribute("server").Value,
                            $"{tag.Parent.Attribute("name").Value}.{tag.Attribute("node").Value}",
                             value 
                        );
                    }
        
                }, 
                (id, tag) => DAItemChangedObservable.Create<object>(new DAItemGroupArguments(
                    tag.Parent.Element("da").Attribute("endpoint").Value,
                    tag.Parent.Element("da").Attribute("server").Value,
                    $"{tag.Parent.Attribute("name").Value}.{tag.Attribute("node").Value}",
                    int.Parse(GetAttribute(tag, "updateRate")),
                    id)
                 )
                    .Select(val => new SubValue { Quality = val.Vtq.HasValue ? val.Vtq.Quality== DAQualities.GoodNonspecific : false  , Value = val?.Vtq?.Value })
             );
        
            
        }



        // configured ua client using rx
        static async Task UaRx(string group)
        {
            await Rx(group,
                (tag) => (value) =>
                {
                    using (var uaClient = new EasyUAClient())
                    {
                        uaClient.Isolated = true;

                        var uaConfig = tag.Parent.Element("ua");
                        uaClient.WriteValue(
                            new UAWriteValueArguments(
                               uaConfig.Attribute("endpoint").Value,
                               $"ns=2;s={tag.Parent.Attribute("name").Value}.{ tag.Attribute("node").Value}",
                               value
                            )
                        );
                    }
                        
                    
                }, (id, tag) => 
                    UaObserver.UaObservable(
                        id,
                        tag.Parent.Element("ua").Attribute("endpoint").Value,
                        $"ns=2;s={tag.Parent.Attribute("name").Value}.{tag.Attribute("node").Value}",
                        int.Parse(GetAttribute(tag, "updateRate")),
                        tag.Attribute("type").Value
                    ).Select(val=> new SubValue {  Quality = val.Succeeded, Value = val?.AttributeData?.Value })
                );
        }

        class SubValue
        {
            public object Value { get; set; }
            public bool Quality { get; set; }
        }

        // configured ua client using rx
        static async Task Rx(string group, Func<XElement, Action<object>> write, Func<string, XElement, IObservable<SubValue>> subscriber)
        {
            Clear();
            using (new ConsoleColourer(White, Black))
                Write($"{group} ");
            using (new ConsoleColourer(Yellow, Black))
                WriteLine("Enter id <space> value to write a value,  Return to quit");


            
            new ConsolePositioner(0, 1, BufferWidth);
            Write("> ");

            var config = XDocument.Load("dms.xml");

            var cols = config.Root.Elements("group").Single(e=>e.Attribute("name").Value== group)
                .Elements("col").Select((XElement col, int index)=> new { col, index }).ToList();

            using (new ConsoleColourer(Cyan, Black))
                cols.ForEach(c => TagsDisplay(-1, c.index, c.col));


            var subs = cols.SelectMany(c =>
            {
                var tagconfig = c.col
                    .Elements("tags")
                    .SelectMany(tags => tags.Elements("tag").Select((XElement tag, int index)=> Tuple.Create( tag, index )));
                var tagConfigs = tagconfig.Select((Tuple<XElement, int> tag, int index) =>
                new { tag = tag.Item1, id = $"{c.index}.{index}", index = tag.Item2,  line = index + tag.Item1.Parent.Parent.Elements("tags").ToList().IndexOf(tag.Item1.Parent) + 1 }).ToList();

                tagConfigs.ForEach(t => TitleDisplay(t.line, c.index, t.tag));
                using(new ConsoleColourer(White, Black))
                    tagConfigs.Where(t=>t.index==0).ToList().ForEach(t => TagsDisplay(t.line-1, c.index, t.tag.Parent));


                return tagConfigs.Select(
                    (t, index) => {
                        return new
                        {
                            Id = t.id,
                            Writer = write(t.tag),
                            Subscription = subscriber(t.id, t.tag)
                                .DistinctUntilChanged(v => $"{v.Quality}:{v.Value}")
                                .Subscribe(
                                    val => PosDisplay(t.id, t.line, c.index, t.tag, val.Value,val.Quality),
                                    (ex) => ExceptionDisplay(t.id, t.line, c.index, t.tag, ex),
                                    () => { }
                                )
                        };
                    }
                ).ToList();
            }

            ).ToList(); ;

            var myTags = config.Root
                .Elements("col")
                .SelectMany(col => col.Elements("tags"))
                .SelectMany(tags => tags.Elements("tag"));

            var key = await In.ReadLineAsync();

            while (key != "")
            {
                lock (consoleLock)
                {
                    var vals = key.Split(' ');
                    try
                    {
                        lock (consoleLock)
                        {
                            new ConsolePositioner(0, 1, BufferWidth * 2);
                            Write("> ");
                            //WindowTop = 0;
                        }

                        subs.Single(s => s.Id == vals[0]).Writer(new string(key.Skip(vals[0].Length + 1).ToArray()));

                    }
                    catch (Exception ex)
                    {
                        lock (consoleLock)
                            using (new ConsolePositioner(0, 2, BufferWidth))
                            using (new ConsoleColourer(Black, Yellow))
                                WriteLine(ex.GetType().Name);
                    }

                }
                key = await In.ReadLineAsync();
            }
            Clear();
            subs.ForEach(s => s.Subscription.Dispose());
            
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
                    tag => UaObserver.UaObservable(
                        tag.Attribute("id").Value,
                        tag.Parent.Element("ua").Attribute("endpoint").Value,
                        $"ns=2;s={tag.Parent.Attribute("name").Value}.{tag.Attribute("node").Value}",
                        int.Parse(GetAttribute(tag, "updateInterval")),
                        tag.Attribute("type").Value
                    )
                        .Subscribe(
                            val => {
                                var uaConfig = tag.Parent.Element("ua");
                                lock(locker)
                                    using (System.IO.StreamWriter file = new System.IO.StreamWriter(logFileName, true))
                                    {

                                        var log = val.Succeeded
                                             ? string.Format("{0}, {1}, {2}, {3}, {4}, {5}, {6}", tag.Attribute("id").Value, uaConfig.Attribute("endpoint").Value, tag.Attribute("name").Value, string.Format("ns=2;s={0}.{1}", tag.Parent.Attribute("name").Value, tag.Attribute("node").Value), val.AttributeData.Value, val.AttributeData.HasGoodStatus, DateTime.Now.ToLongTimeString())
                                             : string.Format("{0}, {1}, {2}, {3}, {4}, {5}, {6}", tag.Attribute("id").Value, uaConfig.Attribute("endpoint").Value, tag.Attribute("name").Value, string.Format("ns=2;s={0}.{1}", tag.Parent.Attribute("name").Value, tag.Attribute("node").Value), "", false, DateTime.Now.ToLongTimeString());
                                        file.WriteLine(log);
                                        WriteLine(log);
                                    }

                                
                            }, 
                            //(ex) => ExceptionDisplay(tag, ex),
                            () => { }
                        )
                ).ToList();


            var key = ReadLine();
            subs.ForEach(s => s.Dispose());

        }



        // ua simulator hard coded + hard coded config
        static void UaRxSim()
        {

            Clear();
            using (new ConsoleColourer(Yellow, Black))
                WriteLine("reactive UA simulation! Press a key to quit.");
            

            var uaClient = new EasyUAClient();

            var rxUaSubscription = UAMonitoredItemChangedObservable.Create<int>(new EasyUAMonitoredItemArguments(
               "uaState",
               "opc.tcp://127.0.0.1:49320/",
               "ns=2;s=Channel1.Device1.Tag2",
               0)
             )
             .Subscribe(val =>
             {
                 WriteLine(string.Format("wrote {0} to {1} on {2} being {3}", val.AttributeData.Value, "ns=2;s=Channel1.Device1.Tag1", "ns=2;s=Channel1.Device1.Tag2", val.AttributeData.Value));
                 uaClient.WriteValue(
                     new UAWriteValueArguments(
                         "opc.tcp://127.0.0.1:49320/",
                         "ns=2;s=Channel1.Device1.Tag1",
                         val.AttributeData.Value
                     )
                 );

             }
         );

            var key = ReadKey();

            rxUaSubscription.Dispose();
            uaClient.Dispose();
        }

    

    }


    //static class create a ua IObservable of multiple types
    public static class UaObserver
    {
        static public IObservable<EasyUAMonitoredItemChangedEventArgs> UaObservable(object status, string endpoint, string node, int updateInterval, string type)
        {
            var args = new EasyUAMonitoredItemArguments(
                status,
                endpoint,
                node,
                updateInterval
            );

            return UAMonitoredItemChangedObservable.Create<object>(args);
            
        }
    } 

    public class ConsolePositioner : IDisposable
    {
        int _top, _left;
        public ConsolePositioner(int left, int top, int blank)
        {
            _top= CursorTop;
            _left = CursorLeft;
            SetCursorPosition(left,top);
            Write(new string(' ', blank));
            SetCursorPosition(left, top);
            //SetWindowPosition(0, 0);
        }

        public void Dispose()
        {
            SetCursorPosition(_left, _top);
        }
    }



    public class ConsoleColourer : IDisposable
    {
        ConsoleColor _foreGround, _backGround;
        public ConsoleColourer( ConsoleColor foreground, ConsoleColor backGround)
        {
            _foreGround = ForegroundColor;
            _backGround = BackgroundColor;
            ForegroundColor= foreground;
            BackgroundColor = backGround;
        }

        public void Dispose()
        {
            ForegroundColor= _foreGround;
            BackgroundColor = _backGround;
        }
    }
}
