using Recommenders;
using Recommenders.RWRBased;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace TweetRecommender {
    public enum Methodology { BASELINE, INCL_FRIENDSHIP, PROPOSED2 }
    public enum EvaluationMetric { HIT, AVGPRECISION }

    public class Program {
        public static Dictionary<EvaluationMetric, double> makeConclusion(HashSet<long> testSet, List<KeyValuePair<long, double>> recommendation) {
            Dictionary<long, int> ranking = new Dictionary<long, int>();
            Dictionary<long, double> score = new Dictionary<long, double>();

            int nRecommendation = recommendation.Count;
            int nHits = 0;
            double sumPrecision = 0;
            for (int i = 0; i < nRecommendation; i++) {
                if (recommendation[i].Value == 0)
                    break;
                long recommendedTweetId = recommendation[i].Key;
                double rankOfTweet = recommendation[i].Value;
                if (testSet.Contains(recommendedTweetId)) {
                    // Record ranking of the current hit
                    ranking.Add(recommendedTweetId, i);
                    score.Add(recommendedTweetId, rankOfTweet);

                    // Measure several values for evaluation metrics
                    nHits += 1;
                    sumPrecision += (double)nHits / i;
                }
            }

            // Evaluation metrics
            var result = new Dictionary<EvaluationMetric, double>();
            result.Add(EvaluationMetric.HIT, nHits);                                            // The number of hits
            result.Add(EvaluationMetric.AVGPRECISION, (nHits == 0) ? 0 : sumPrecision / nHits); // Average Precision

            // Console write
            Console.WriteLine("\t* Cross validation result");
            Console.WriteLine("\t\t- # of hits: " + result[EvaluationMetric.HIT]);
            Console.WriteLine("\t\t- # of testset: " + testSet.Count);
            Console.WriteLine("\t\t- Average precision: " + result[EvaluationMetric.AVGPRECISION]);
            if (ranking.Count > 0)
                Console.WriteLine("\t* Ranking per hit");
            foreach (KeyValuePair<long, int> entry in ranking) {
                string percentage = ((float)entry.Value * 100 / nRecommendation).ToString("#.##");
                Console.WriteLine("\t\t- " + entry.Key + "(" + score[entry.Key] + "):\t"
                    + entry.Value + "/" + nRecommendation + " (" + percentage + "%)");
            }

            return result;
        }
        
        // Argument #0: path of directory that containes SQLite DB files
        // Argument #1: methodology (ex. 0: baseline)
        // Argument #2: number of folds
        // Argument #3: number of iterations for Random Walk with Restart algorithm
        public static void Main(string[] args) {
            Console.WriteLine("Random Walk with Restart based Recommendation");
            Console.WriteLine(DateTime.Now);
            Console.WriteLine();
            Stopwatch stopwatch = Stopwatch.StartNew();

            // File writer with format: "EgoUserId \t Methodology \t Metric1 \t Metric2 \t ... \n"
            StreamWriter file = new StreamWriter(args[0] + "result.dat", true);

            // Graph configuration methodology
            Methodology methodology = (Methodology)int.Parse(args[1]);

            string[] sqliteDBs = Directory.GetFiles(args[0], "*.sqlite");
            foreach (string dbPath in sqliteDBs) {
                // Get ego user's name
                string egoUser = Path.GetFileNameWithoutExtension(dbPath);

                // Initialize values per evaludation metric
                var validationResult = new Dictionary<EvaluationMetric, double>();
                foreach (EvaluationMetric metric in Enum.GetValues(typeof(EvaluationMetric)))
                    validationResult.Add(metric, 0d);

                // K-Fold Cross Validation
                int nFolds = int.Parse(args[2]);
                for (int fold = 0; fold < nFolds; fold++) {
                    // Load graph information from database and then configurate the graph
                    DataLoader loader = new DataLoader(dbPath, nFolds);
                    loader.graphConfiguration(methodology, fold);

                    // Nodes and edges of graph
                    Dictionary<int, Node> nodes = loader.allNodes;
                    Dictionary<int, List<ForwardLink>> edges = loader.allLinks;

                    // Make a graph structure to run Random Walk with Restart algorithm
                    Graph graph = new Graph(nodes, edges);
                    graph.buildGraph();

                    // Get recommendation list
                    Recommender recommender = new Recommender(graph);
                    var recommendation = recommender.Recommendation(0, 0.15f, int.Parse(args[3]));

                    // Print out validation result
                    var result = makeConclusion(loader.testSet, recommendation);
                    foreach (EvaluationMetric metric in Enum.GetValues(typeof(EvaluationMetric)))
                        validationResult[metric] += result[metric];
                }

                // Write the result of this ego network to file
                Console.WriteLine("Result on " + egoUser);
                file.Write(egoUser + "\t" + args[1] + "\t" + args[2] + "\t" + args[3]);
                foreach (EvaluationMetric metric in Enum.GetValues(typeof(EvaluationMetric))) {
                    Console.WriteLine("* " + metric.ToString() + ": " + validationResult[metric]);
                    if (metric == EvaluationMetric.HIT)
                        file.Write("\t" + (int)validationResult[metric]);
                    else
                        file.Write("\t" + validationResult[metric]);
                }
                Console.WriteLine();
                file.WriteLine();
            }

            file.Close();
            stopwatch.Stop();
            Tools.printExecutionTime(stopwatch);
            Console.WriteLine("Finished!");
        }
    }
}
