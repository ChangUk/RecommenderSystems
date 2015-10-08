using Recommenders;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace TweetRecommender {
    public class Program {
        // To limit the number of multithreading concurrency
        public static Semaphore semaphore = new Semaphore(10, 10);

        // To avoid file writer collision
        public static Object locker = new Object();

        // Path of directory that contains data files (*.sqlite)
        public static string dirData;

        public static void Main(string[] args) {
            Console.WriteLine("RWR-based Recommendation (" + DateTime.Now.ToString() + ")\n");
            Stopwatch stopwatch = Stopwatch.StartNew();

            dirData = @args[0] + Path.DirectorySeparatorChar;           // Path of directory that containes SQLite DB files
            Methodology methodology = (Methodology)int.Parse(args[1]);  // Graph configuration methodology (ex. 0: baseline)
            int nFolds = int.Parse(args[2]);                            // Number of folds
            int nIterations = int.Parse(args[3]);                       // Number of iterations for RWR

            // Run experiments using multi-threading
            string[] sqliteDBs = Directory.GetFiles(dirData, "*.sqlite");
            List<Thread> threadList = new List<Thread>();
            foreach (string dbFile in sqliteDBs) {
                Thread thread = new Thread(new ParameterizedThreadStart(Experiment.runKFoldCrossValidation));
                ThreadParams parameters = new ThreadParams(dbFile, methodology, nFolds, nIterations);
                thread.Start(parameters);
                threadList.Add(thread);
            }

            foreach (Thread thread in threadList)
                thread.Join();

            stopwatch.Stop();
            Tools.printExecutionTime(stopwatch);
            Console.WriteLine("Finished!");
        }
    }
}
