using Recommenders.RWRBased;
using System;
using System.Collections.Generic;
using System.IO;

namespace TweetRecommender {
    public enum Methodology { BASELINE, INCL_FRIENDSHIP, INCL_ALLFOLLOWSHIP, INCL_AUTHORSHIP }
    public enum EvaluationMetric { HIT, AVGPRECISION }

    public struct ThreadParams {
        public string dbFile;
        public Methodology methodology;
        public int nFolds;
        public int nIterations;

        public ThreadParams(string dbFile, Methodology methodology, int nFolds, int nIterations) {
            this.dbFile = dbFile;
            this.methodology = methodology;
            this.nFolds = nFolds;
            this.nIterations = nIterations;
        }
    }

    public class Experiment {
        public static void runKFoldCrossValidation(object parameters) {
            try {
                Program.semaphore.WaitOne();

                // Setting environment for experiments
                ThreadParams p = (ThreadParams)parameters;
                string dbFile = p.dbFile;
                Methodology methodology = p.methodology;
                int nFolds = p.nFolds;
                int nIterations = p.nIterations;

                // Get ego user's ID and his like count
                long egoUser = long.Parse(Path.GetFileNameWithoutExtension(dbFile));
                int cntLikes = 0;

                // Final result to put the experimental result per fold together
                var finalResult = new Dictionary<EvaluationMetric, double>();
                foreach (EvaluationMetric metric in Enum.GetValues(typeof(EvaluationMetric)))
                    finalResult.Add(metric, 0d);

                // Need to avoid the following error: "Collection was modified; enumeration operation may not execute"
                List<EvaluationMetric> metrics = new List<EvaluationMetric>(finalResult.Keys);

                // K-Fold Cross Validation
                for (int fold = 0; fold < nFolds; fold++) {
                    // Load graph information from database and then configurate the graph
                    DataLoader loader = new DataLoader(dbFile, nFolds);
                    if (fold == 0) {
                        if (loader.checkEgoNetworkValidation() == false)
                            return;
                        cntLikes = loader.cntLikesOfEgoUser;
                    }
                    loader.graphConfiguration(methodology, fold);

                    // Nodes and edges of graph
                    Dictionary<int, Node> nodes = loader.allNodes;
                    Dictionary<int, List<ForwardLink>> edges = loader.allLinks;

                    // Make a graph structure to run Random Walk with Restart algorithm
                    Graph graph = new Graph(nodes, edges);
                    graph.buildGraph();

                    // Get recommendation list
                    Recommender recommender = new Recommender(graph);
                    var recommendation = recommender.Recommendation(0, 0.15f, nIterations);

                    // Get evaluation result
                    int nHits = 0;
                    double sumPrecision = 0;
                    for (int i = 0; i < recommendation.Count; i++) {
                        if (loader.testSet.Contains(recommendation[i].Key)) {
                            nHits += 1;
                            sumPrecision += (double)nHits / (i + 1);
                        }
                    }

                    // Add current result to final one
                    foreach (EvaluationMetric metric in metrics) {
                        switch (metric) {
                            case EvaluationMetric.HIT:
                                finalResult[metric] += nHits; break;
                            case EvaluationMetric.AVGPRECISION:
                                finalResult[metric] += (nHits == 0) ? 0 : sumPrecision / nHits; break;
                        }
                    }
                }

                lock (Program.locker) {
                    // Write the result of this ego network to file
                    StreamWriter logger = new StreamWriter(Program.dirData + "result.dat", true);
                    logger.Write(egoUser + "\t" + (int)methodology + "\t" + nFolds + "\t" + nIterations);
                    foreach (EvaluationMetric metric in metrics) {
                        switch (metric) {
                            case EvaluationMetric.HIT:
                                logger.Write("\t" + (int)finalResult[metric] + "\t" + cntLikes); break;
                            case EvaluationMetric.AVGPRECISION:
                                logger.Write("\t" + (finalResult[metric] / nFolds)); break;
                        }
                    }
                    logger.WriteLine();
                    logger.Close();
                }
            } finally {
                Program.semaphore.Release();
            }
        }

        //public static Dictionary<EvaluationMetric, double> getResult(HashSet<long> testSet, List<KeyValuePair<long, double>> recommendation) {
        //    Dictionary<long, int> ranking = new Dictionary<long, int>();
        //    Dictionary<long, double> score = new Dictionary<long, double>();

        //    int nHits = 0;
        //    double sumPrecision = 0;
        //    for (int i = 0; i < recommendation.Count; i++) {
        //        double rankOfTweet = recommendation[i].Value;
        //        if (testSet.Contains(recommendation[i].Key)) {      // recommendation[i]: recommended item
        //            // Record ranking of the current hit
        //            ranking.Add(recommendation[i].Key, i);
        //            score.Add(recommendation[i].Key, rankOfTweet);

        //            nHits += 1;
        //            sumPrecision += (double)nHits / (i + 1);
        //        }
        //    }

        //    var result = new Dictionary<EvaluationMetric, double>();
        //    result.Add(EvaluationMetric.HIT, nHits);                                            // The number of hits
        //    result.Add(EvaluationMetric.AVGPRECISION, (nHits == 0) ? 0 : sumPrecision / nHits); // Average Precision

        //    // Console write
        //    Console.WriteLine("\t* Cross validation result");
        //    Console.WriteLine("\t\t- # of hits: " + result[EvaluationMetric.HIT]);
        //    Console.WriteLine("\t\t- # of testset: " + testSet.Count);
        //    Console.WriteLine("\t\t- Average precision: " + result[EvaluationMetric.AVGPRECISION]);
        //    if (ranking.Count > 0)
        //        Console.WriteLine("\t* Ranking per hit");
        //    foreach (KeyValuePair<long, int> entry in ranking) {
        //        string percentage = ((float)entry.Value * 100 / recommendation.Count).ToString("#.##");
        //        Console.WriteLine("\t\t- " + entry.Key + "(" + score[entry.Key] + "):\t"
        //            + entry.Value + "/" + recommendation.Count + " (" + percentage + "%)");
        //    }

        //    return result;
        //}
    }
}
