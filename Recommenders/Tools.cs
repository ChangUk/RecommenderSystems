using System;
using System.Diagnostics;

namespace Recommenders {
    public class Tools {
        public static void printExecutionTime(Stopwatch stopwatch) {
            var timespan = TimeSpan.FromMilliseconds(stopwatch.ElapsedMilliseconds);
            Console.WriteLine("Execution time: " + timespan.ToString());
        }
    }
}
