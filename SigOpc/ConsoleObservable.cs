using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SigOpc
{
    public static class ConsoleObservable
    {
        public static IObservable<string> ConsoleInputObservable(
    IScheduler scheduler = null)
        {
            scheduler = scheduler ?? Scheduler.Default;
            return Observable.Create<string>(o =>
            {
                return scheduler.ScheduleAsync(async (ctrl, ct) =>
                {
                    while (!ct.IsCancellationRequested)
                    {
                        System.Diagnostics.Debug.WriteLine($"reading line {DateTime.Now.ToLongTimeString()}");
                        var next = Console.ReadLine();
                        if (ct.IsCancellationRequested)
                            return;
                        using(new Timer($"readLine -- {next} "))
                            o.OnNext(next);
                        await ctrl.Yield();
                    }
                });
            });
        }
    }

}
