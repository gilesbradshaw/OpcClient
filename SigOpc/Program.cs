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
using System.Reactive.Concurrency;
using System.Xml.Linq;
using System.IO;
using static System.Console;
using static System.ConsoleColor;
using System.Reactive.Disposables;



namespace SigOpc
{
    using static TagDisplay;
    using static XmlUtilities;

    class Program
    {
       

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
            var consoleO = ConsoleObservable.ConsoleInputObservable().Publish();
            using (consoleO.Connect())
            {
                string next;
                while ((next = await consoleO.Take(1)) != "q")
                {

                    //pick an option based on command line
                    try
                    {
                        switch (next.ToLower())
                        {
                            case "e":
                                await Ex.Do(consoleO);
                                break;
                            case "dep":
                                await Dependency.Do(consoleO);
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
                }
            } 
            System.Threading.Thread.Sleep(2000);

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
                    tag => 
                        UAMonitoredItemChangedObservable.Create<object>(new EasyUAMonitoredItemArguments(
                            tag.Attribute("id").Value,
                            GetElementAttribute(tag, "ua", "endpoint"),
                            $"ns=2;s={tag.Parent.Attribute("name").Value}.{tag.Attribute("node").Value}",
                            int.Parse(GetAttribute(tag, "updateInterval"))
                        ))
                        .Subscribe(
                            val => {
                                
                                lock(locker)
                                    using (System.IO.StreamWriter file = new System.IO.StreamWriter(logFileName, true))
                                    {

                                        var log = val.Succeeded
                                             ? string.Format("{0}, {1}, {2}, {3}, {4}, {5}, {6}", tag.Attribute("id").Value, GetElementAttribute(tag, "ua", "endpoint"), tag.Attribute("name").Value, string.Format("ns=2;s={0}.{1}", tag.Parent.Attribute("name").Value, tag.Attribute("node").Value), val.AttributeData.Value, val.AttributeData.HasGoodStatus, DateTime.Now.ToLongTimeString())
                                             : string.Format("{0}, {1}, {2}, {3}, {4}, {5}, {6}", tag.Attribute("id").Value, GetElementAttribute(tag, "ua", "endpoint"), tag.Attribute("name").Value, string.Format("ns=2;s={0}.{1}", tag.Parent.Attribute("name").Value, tag.Attribute("node").Value), "", false, DateTime.Now.ToLongTimeString());
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

    class creator
    {
        bool disposed = false;
        public Action create { get; set; }
        public Action destroy { get; set; }
        public void dispose()
        {
            lock (this)
            {
                if (!disposed)
                {
                    disposed = true;
                    destroy();
                }

            }
        }
    }
    public class ItemValue
    {
        public object val { get; set; }
        public String Id { get; set; }
        public int Line { get; set; }
        public int Index { get; set; }
        public XElement Tag { get; set; }
        public Object Value { get; set; }
        public bool Quality { get; set; }

    }

    
}
