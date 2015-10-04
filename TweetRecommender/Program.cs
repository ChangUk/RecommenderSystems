using Recommenders;
using Recommenders.RWRBased;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace TweetRecommender {
    public class Program {
        static void Main(string[] args) {
            Stopwatch stopwatch = Stopwatch.StartNew();

            string[] sqliteDBs = Directory.GetFiles(args[0], "*.sqlite");
            foreach (string dbPath in sqliteDBs) {
                // Load graph information from database and then configurate the graph
                DataLoader loader = new DataLoader(dbPath);
                loader.graphConfiguration_baseline();
                Dictionary<int, Node> nodes = loader.allNodes;
                Dictionary<int, List<ForwardLink>> edges = loader.allLinks;

                // Make a graph structure to run Random Walk with Restart algorithm
                Graph graph = new Graph(nodes, edges);
                graph.buildGraph();

                // Get recommendation list
                Recommender recommender = new Recommender(graph);
                var recommendation = recommender.Recommendation(0, 0.15f, int.Parse(args[1]), int.Parse(args[2]));
                foreach (KeyValuePair<long, double> item in recommendation)
                    Console.WriteLine(item.Key + "\t" + item.Value);
            }
            
            stopwatch.Stop();
            Tools.printExecutionTime(stopwatch);
            Console.WriteLine("Finished!");
        }
    }
}
