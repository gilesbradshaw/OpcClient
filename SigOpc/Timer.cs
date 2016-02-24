using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SigOpc
{
    public class Timer : IDisposable
    {
        string _title;
        DateTime _start;
        public Timer(string title)
        {
            _title = title;
            _start = DateTime.Now;
            System.Diagnostics.Debug.WriteLine($"{title} started {_start.ToLongTimeString()}");
        }

        public void Dispose()
        {
            var end = DateTime.Now;
            System.Diagnostics.Debug.WriteLine($"{_title} ended {end.ToLongTimeString()} {(end - _start).TotalSeconds}");
        }
    }
}
