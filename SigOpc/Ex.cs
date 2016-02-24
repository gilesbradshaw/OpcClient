using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Console;
using static System.ConsoleColor;
using System.Reactive.Linq;
using System.Xml.Linq;

using OpcLabs.BaseLib.ComInterop;
using OpcLabs.EasyOpc;
using OpcLabs.EasyOpc.DataAccess;
using OpcLabs.EasyOpc.DataAccess.OperationModel;
using OpcLabs.EasyOpc.DataAccess.Reactive;
using OpcLabs.EasyOpc.UA;
using OpcLabs.EasyOpc.UA.OperationModel;
using OpcLabs.EasyOpc.UA.Reactive;


namespace SigOpc
{
    using System.Reactive.Concurrency;
    using System.Reactive.Disposables;
    using static TagDisplay;
    using static XmlUtilities;

    public class Ex
    {
        public static async Task Do()
        {
            Clear();
            using (new ConsoleColourer(Yellow, Black))
                WriteLine("[u(a) | d(a) <group>] | q(uit)");

            new ConsolePositioner(0, 1, BufferWidth);
            Write("> ");

            var consoleO = ConsoleObservable.ConsoleInputObservable().Publish();
            using (consoleO.Connect())
            {


                var config = Config();
                
                // open or quit commands
                var os = consoleO
                    .Where(s => s != null)
                    .Select(s=>s.Trim())
                    .TakeWhile(s => !s.StartsWith("q"))
                    .Where(s => s.StartsWith("d") || s.StartsWith("u"))
                    .Select(command => {
                        var commands = command.Split(' ');
                        var group = commands.Length>1 ? commands[1] : "";
                        var cols = Collections(config, group);
                        return  new Subs
                        {
                            command = command,
                            collections = cols,
                            tags = cols.SelectMany(c =>
                                c.Col
                                    .Elements("tags")
                                    .SelectMany(tags => tags.Elements("tag").Select((XElement tag, int index) => Tuple.Create(tag, index)))
                                    .Select((Tuple<XElement, int> tag, int index) =>
                                        new Sub
                                        {
                                            tag = tag.Item1,
                                            id = $"{c.Index}.{index}",
                                            column = c.Index,
                                            index = tag.Item2,
                                            line = index + tag.Item1.Parent.Parent.Elements("tags").ToList().IndexOf(tag.Item1.Parent) + 1
                                        }
                                    )
                                )


                        };

                        //return command;
                    })
                    .Publish();
                var validCommands = os.Where(s => s.command.Split(' ').Length > 1);
                var invalidCommands = os.Where(s => s.command.Split(' ').Length == 1);
                var disposables = new List<IDisposable>
                {
                    os.Connect(),
                    validCommands.Subscribe(subs=> {
                        Clear();
                        using (new ConsoleColourer(Cyan, Black))
                            subs.collections.ToList().ForEach(c => TagsDisplay(-1, c.Index, c.Col));

                    }),
                    validCommands.Subscribe(s=>SubTitle(s)),
                    invalidCommands.Subscribe( s=> {
                        DisplayException($"valid groups are {string.Join(" | " ,config.Root.Elements("group").Select(g=>g.Attribute("name").Value).ToArray())}");
                    }),
                    Reader(validCommands).Subscribe(v => PosDisplay(v.Id, v.Line, v.Index, v.Tag, v.Value, v.Quality)),
                    Commander(validCommands, consoleO, s=> s.First() == "w").Subscribe(subsCommand => WithException(()=> RxxWriter(subsCommand))),
                    consoleO.Subscribe(c =>
                        {
                            lock (consoleLock)
                            {
                                new ConsolePositioner(0, 1, BufferWidth);
                                Write("> ");
                                //WindowTop = 0;
                            }
                        }),
                    Commander(validCommands, consoleO, s => s.First() == "s").Select(subsCommand => Handle(()=>Scanner(subsCommand))).Switch().Subscribe(
                        s =>
                            {
                                lock (consoleLock)
                                {
                                    using (new ConsolePositioner(0, 3, BufferWidth))
                                            Write(s.Item1);
                                    if (s.Item2 != null)
                                        DisplayException(s.Item2);
                                }
                            }
                        ),
                    Commander(os, consoleO, s => s.First() != "s" && s.First() != "w").Subscribe(s=> DisplayException($"command is unrecognised"))
                }.Disposer();
                using (disposables) await os;                   
            }
            
        }
        static IObservable<string> Scanner(SubsCommand subsCommand)
        {
            if(!subsCommand.Command.Any())
                return Observable.Return("");
            var id = subsCommand.Command.First();
            var tag = subsCommand.Subs.tags.Single(t => t.id == id).tag;
            if (subsCommand.Subs.command.StartsWith("u"))
                return Ua(id, tag).Select(v => v.val.ToString());
            else
                return Observable.Never<string>();

        }


        static void SubTitle(Subs pageCommand)
        {
            var command = pageCommand.command.Split(' ');
            var group = command.Skip(1).FirstOrDefault();
            using (new ConsoleColourer(White, Black))
                Write($"{group} ");
            using (new ConsoleColourer(Yellow, Black))
                WriteLine("[w(rite) <id> <value>] | [s(tatus) <id>] | [u(a) <group>] | [d(a) <group>] | q(uit)");

            new ConsolePositioner(0, 1, BufferWidth);
            Write("> ");
        }

        static IObservable<SubsCommand> Commander(IObservable<Subs> os, IObservable<string> consoleO, Func<IEnumerable<string>, bool> test)
        {
            return os.Select(subs => consoleO.Select(s => s.Split(' ')).Where(s => test(s)).Select(s => s.Skip(1)).Select(s => new SubsCommand { Subs = subs, Command = s })).Switch();
        }

        static IObservable<ItemValue> Reader(IObservable<Subs> os)
        {
            return os
                .Select(subs =>
                {
                    using (new Timer("Reader pagecommand"))
                    {
                        if (subs.command.Split(' ')[0] == "d")
                            return DaRxxReader(subs);
                        else
                            return UaRxxReader(subs);
                    }

                }).Switch();
        }

        static void UaRxxWriter(XElement tag, object value)
        {
                    using (var uaClient = new EasyUAClient())
                    {
                        uaClient.WriteValue(
                            new UAWriteValueArguments(
                               GetElementAttribute(tag, "ua", "endpoint"),
                               $"ns=2;s={tag.Parent.Attribute("name").Value}.{ tag.Attribute("node").Value}",
                               value
                            )
                        );
                    }
            
        }
        // configured da client using rx
        static IObservable<ItemValue> UaRxxReader(Subs subs)
        {
            using(new Timer("UaRxxReader"))
                return RxxReader(subs,Ua);


        }

        static IObservable<SubValue> Ua(string id, XElement tag)
        {
            return UAMonitoredItemChangedObservable.Create<object>(new EasyUAMonitoredItemArguments(
                id,
                GetElementAttribute(tag, "ua", "endpoint"),
                $"ns=2;s={tag.Parent.Attribute("name").Value}.{tag.Attribute("node").Value}",
                int.Parse(GetAttribute(tag, "updateRate"))

            )).Select(val => new SubValue { val = val, Quality = val.Succeeded, Value = val?.AttributeData?.Value });
        }


        static void DaRxxWriter(XElement tag, object value)
        {
            using (var client = new EasyDAClient())
            {

                client.WriteItemValue(
                    GetElementAttribute(tag, "da", "endpoint"),
                    GetElementAttribute(tag, "da", "server"),
                    $"{tag.Parent.Attribute("name").Value}.{tag.Attribute("node").Value}",
                     value
                );
            }

        }
        // configured da client using rx
        static IObservable<ItemValue> DaRxxReader(Subs subs)
        {

            return RxxReader(subs,
                (id, tag) => DAItemChangedObservable.Create<object>(new DAItemGroupArguments(
                    GetElementAttribute(tag, "da", "endpoint"),
                    GetElementAttribute(tag, "da", "server"),
                    $"{tag.Parent.Attribute("name").Value}.{tag.Attribute("node").Value}",
                    int.Parse(GetAttribute(tag, "updateRate")),
                    id)
                 )
                    .Select(val => new SubValue { Quality = val.Vtq.HasValue ? val.Vtq.Quality == DAQualities.GoodNonspecific : false, Value = val?.Vtq?.Value })
             );


        }
        static void RxxWriter(SubsCommand subsCommand)
        {
            var xElement = subsCommand.Subs.tags.Single(s => s.id == subsCommand.Command.First()).tag;
            var value = string.Join(" ", subsCommand.Command.Skip(1).ToArray());
            if (subsCommand.Subs.command.StartsWith("u"))
                UaRxxWriter(xElement, value);
            if (subsCommand.Subs.command.StartsWith("s"))
                DaRxxWriter(xElement, value);



        }


        static IObservable<ItemValue> RxxReader(Subs subs, Func<string, XElement, IObservable<SubValue>> subscriber)
        {
            


            subs.tags.ToList().ForEach(t => TitleDisplay(t.line, t.column, t.tag));

            using (new ConsoleColourer(White, Black))
                subs.tags.Where(t => t.index == 0).ToList().ForEach(t => TagsDisplay(t.line - 1, t.column, t.tag.Parent));


            return Observable.Merge(subs.tags.AsParallel().Select(
                (t, index) =>
                subscriber(t.id, t.tag)
                    .DistinctUntilChanged(v => $"{v.Quality}:{v.Value}")
                    .Select(val => new ItemValue
                    {
                        val = val.val,
                        Id = t.id,
                        Line = t.line,
                        Index = t.column,
                        Tag = t.tag,
                        Value = val.Value,
                        Quality = val.Quality
                    })
            ).ToList()).SubscribeOn(Scheduler.Default); 
        }

        class SubValue
        {
            public object val { get; set; }
            public object Value { get; set; }
            public bool Quality { get; set; }
        }
        class SubsCommand
        {
            public IEnumerable<string> Command { get; set; }
            public Subs Subs { get; set; }
        }
        class Subs
        {
            public string command { get; set; }
            public IEnumerable<Collection> collections { get; set; }
            public IEnumerable<Sub> tags { get; set; }
        }
        class Sub
        {
            public XElement tag { get; set; }
            public string id { get; set; }
            public int column { get; set; } 
            public int index { get; set; } 
            public int line { get; set; } 
        }
    }
}
