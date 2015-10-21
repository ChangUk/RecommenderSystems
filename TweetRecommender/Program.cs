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

            // Program arguments
            dirData = @args[0] + Path.DirectorySeparatorChar;           // Path of directory that containes SQLite DB files
            int nFolds = int.Parse(args[1]);                            // Number of folds
            int nIterations = int.Parse(args[2]);                       // Number of iterations for RWR

            // Run experiments using multi-threading
            string[] sqliteDBs = Directory.GetFiles(dirData, "*.sqlite");
            List<Thread> threadList = new List<Thread>();

            // Methodology list
            List<Methodology> methodologies = new List<Methodology>();
            methodologies.Add(Methodology.BASELINE);
            methodologies.Add(Methodology.INCL_FRIENDSHIP);
            methodologies.Add(Methodology.ALL);
            methodologies.Add(Methodology.EXCL_AUTHORSHIP);
            methodologies.Add(Methodology.EXCL_FOLLOWSHIP_ON_THIRDPARTY);
            methodologies.Add(Methodology.EXCL_MENTIONCOUNT);

            foreach (string dbFile in sqliteDBs) {
                foreach (Methodology m in methodologies) {
                    Thread thread = new Thread(new ParameterizedThreadStart(Experiment.runKFoldCrossValidation));
                    ThreadParams parameters = new ThreadParams(dbFile, m, nFolds, nIterations);
                    thread.Start(parameters);
                    threadList.Add(thread);
                }
            }

            foreach (Thread thread in threadList)
                thread.Join();

            stopwatch.Stop();
            Tools.printExecutionTime(stopwatch);
            Console.WriteLine("Finished!");
        }
    }
}
