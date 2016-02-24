using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.ConsoleColor;
using static System.Console;
using System.Xml.Linq;
using System.Reactive.Linq;
using System.Reactive.Disposables;

namespace SigOpc
{
    public static class TagDisplay
    {

        //locks access to the console
        public static readonly object consoleLock = new object();

        //writing a value
        public static void Writing(object value)
        {
            using (new ConsoleColourer(Green, Black))
                WriteLine($"writing {value}");
        }

        //Read a value
        public static void Read(object value)
        {
            using (new ConsoleColourer(Magenta, Black))
                WriteLine($"READ: {value}");
        }
        public static void DisplayException(Exception ex)
        {
            DisplayException(ex.GetType().Name);
        }
        public static void DisplayException(string ex)
        {
            lock (consoleLock)
            {
                using (new ConsolePositioner(0, 2, Console.BufferWidth))
                using (new ConsoleColourer(Black, Yellow))
                    Write(ex);
            }
        }

        public static IObservable<Tuple<T, Exception>> Handle<T>(Func<IObservable<T>> observable) {
            try {
                return observable().Select(s => new Tuple<T, Exception>(s, null));
            } catch (Exception ex)
            {
                return Observable.Return(new Tuple<T, Exception>(default(T), ex));
            }
        }

        public static void WithException(Action action)
        {
            try
            {
                (new ConsolePositioner(0, 3, Console.BufferWidth)).Dispose();
                 action();
            } catch(Exception ex)
            {
                DisplayException(ex);
            }
        }
        //displays a tag value at a position
        public static void PosDisplay(string id, int line, int column, XElement config, object value, bool quality)
        {
            if (!quality)
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
        public static void IdDisplay(string id, int line, int column)
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
        public static XElement TitleDisplay(int line, int column, XElement config)
        {

            lock (consoleLock)
            {
                var startCol = column * 80;

                using (new ConsolePositioner(startCol + 5, line + 5, 0))
                {
                    Write(config.Attribute("name").Value);
                }

                return config;
            }
        }

        //displays a tag title
        public static XElement TagsDisplay(int line, int column, XElement config)
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
        public static void ExceptionDisplay(string id, int line, int column, XElement config, Exception ex)
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

        public static IDisposable Disposer(this IEnumerable<IDisposable> disposables)
        {
            return Disposable.Create(() => disposables.ToList().ForEach(d => d.Dispose()));
        }
    }
}
