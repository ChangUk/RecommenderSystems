using Recommenders;
using Recommenders.RWRBased;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace TweetRecommender {
    public class Program {
        static void Main(string[] args) {

            Stopwatch stopwatch = Stopwatch.StartNew();

            long egoUserId = 15108415L;

            // Load graph information from database and then configurate the graph
            DataLoader loader = new DataLoader("C:\\GitHub\\Data\\CNN\\" + egoUserId + ".sqlite", egoUserId);
            loader.graphConfiguration_baseline();
            Dictionary<int, Node> nodes = loader.allNodes;
            Dictionary<int, List<ForwardLink>> edges = loader.allLinks;

            // Make a graph structure to run Random Walk with Restart algorithm
            Graph graph = new Graph(nodes, edges);
            graph.buildGraph();

            // Get recommendation list
            int idxEgoUser = loader.userIDs[egoUserId];
            Recommender recommender = new Recommender(graph);
            var recommendation = recommender.Recommendation(idxEgoUser, 0.15f, 10, 100);
            foreach (KeyValuePair<long, double> item in recommendation)
                Console.WriteLine(item.Key + "\t" + item.Value);

            stopwatch.Stop();
            Tools.printExecutionTime(stopwatch);
            Console.WriteLine("Finished!");
        }
    }
}
