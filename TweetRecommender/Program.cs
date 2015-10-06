using Recommenders;
using Recommenders.RWRBased;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace TweetRecommender {
    public enum RecSys { BASELINE, PROPOSED1, PROPOSED2 }

    public class Program {
        public static List<double> makeConclusion(HashSet<long> testSet, List<KeyValuePair<long, double>> recommendation) {
            Dictionary<long, int> ranking = new Dictionary<long, int>();

            int nHits = 0;
            double sumPrecision = 0;
            for (int i = 0; i < recommendation.Count; i++) {
                if (recommendation[i].Value == 0)
                    break;
                long recommendedTweetId = recommendation[i].Key;
                if (testSet.Contains(recommendedTweetId)) {
                    // Record ranking of the current hit
                    ranking.Add(recommendedTweetId, i);

                    // Measure several values for evaluation metrics
                    nHits += 1;
                    sumPrecision += (double)nHits / i;
                }
            }

            // Evaluation metrics
            List<double> result = new List<double>();
            result.Add(nHits);                  // The number of hits
            double averagePrecision = (nHits == 0) ? 0 : sumPrecision / nHits;
            result.Add(averagePrecision);       // Average Precision

            // Console print
            Console.WriteLine("\tThe result of validation:");
            Console.WriteLine("\t# of Hits: " + nHits);
            Console.WriteLine("\t# of Testset: " + testSet.Count);
            Console.WriteLine("\tAverage Precision: " + averagePrecision);
            foreach (KeyValuePair<long, int> entry in ranking)
                Console.WriteLine("\tHit! " + entry.Key + "\t" + entry.Value);

            return result;
        }
        
        // Argument #0: path of directory that containes SQLite DB files
        // Argument #1: number of folds
        // Argument #2: number of iterations for Random Walk with Restart algorithm
        public static void Main(string[] args) {
            Stopwatch stopwatch = Stopwatch.StartNew();

            // File writer
            StreamWriter file = new StreamWriter(args[0] + "result.dat", true);

            string[] sqliteDBs = Directory.GetFiles(args[0], "*.sqlite");
            foreach (string dbPath in sqliteDBs) {
                // Get ego user's name
                string egoUser = Path.GetFileNameWithoutExtension(dbPath);

                // K-Fold Cross Validation
                int nFolds = int.Parse(args[1]);
                for (int fold = 0; fold < nFolds; fold++) {
                    // Load graph information from database and then configurate the graph
                    DataLoader loader = new DataLoader(dbPath, nFolds);
                    loader.graphConfiguration(RecSys.BASELINE, fold);

                    Dictionary<int, Node> nodes = loader.allNodes;
                    Dictionary<int, List<ForwardLink>> edges = loader.allLinks;

                    // Make a graph structure to run Random Walk with Restart algorithm
                    Graph graph = new Graph(nodes, edges);
                    graph.buildGraph();

                    // Get recommendation list
                    Recommender recommender = new Recommender(graph);
                    var recommendation = recommender.Recommendation(0, 0.15f, int.Parse(args[2]));

                    // Print out validation result
                    var result = makeConclusion(loader.testSet, recommendation);
                    file.Write(RecSys.BASELINE + "\t" + egoUser + "\t" + fold);
                    for (int i = 0; i < result.Count; i++)
                        file.Write("\t" + result[i]);
                    file.WriteLine();
                }
            }

            file.Close();
            stopwatch.Stop();
            Tools.printExecutionTime(stopwatch);
            Console.WriteLine("Finished!");
        }
    }
}
