using Recommenders;
using Recommenders.RWRBased;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace TweetRecommender {
    public class Program {
        private static void printStatistics(List<NodeInfo> userNodeList, List<NodeInfo> itemNodeList) {
            int cntNoForwardLinks = 0;

            Console.WriteLine("\t* # of users: " + userNodeList.Count);
            Console.WriteLine("\t* # of tweets: " + itemNodeList.Count);

            foreach (NodeInfo node in userNodeList) {
                if (node.forwardLinks == null)
                    cntNoForwardLinks += 1;
            }
            Console.WriteLine("\t* # of users having no like history: " + cntNoForwardLinks);
            cntNoForwardLinks = 0;
            foreach (NodeInfo node in itemNodeList)
                if (node.forwardLinks == null)
                    cntNoForwardLinks += 1;
            Console.WriteLine("\t* # of tweets having no like history: " + cntNoForwardLinks);
        }

        private static List<NodeInfo> getRecommendation(List<NodeInfo> fullItemList, int topN) {
            List<NodeInfo> topNRecommendation = new List<NodeInfo>();
            for (int i = 0; i < topN; i++)
                topNRecommendation.Add(fullItemList[i]);
            return topNRecommendation;
        }

        static void Main(string[] args) {
            Stopwatch stopwatch = Stopwatch.StartNew();

            long egoUserId = 533818241L;

            // Load graph information from database and then configurate the graph
            DataLoader loader = new DataLoader("C:\\GitHub\\Data\\CNN\\533818241_1.sqlite", egoUserId);
            loader.graphConfiguration_baseline();
            Dictionary<int, Node> nodes = loader.allNodes;
            Dictionary<int, List<ForwardLink>> edges = loader.allLinks;

            // Make a graph structure to run Random Walk with Restart algorithm
            Graph graph = new Graph(nodes, edges);
            graph.buildGraph();

            // Get recommendation list
            int indEgoUser = loader.userIDs[egoUserId];
            Recommender recommender = new Recommender(graph);
            var recommendation = recommender.Recommendation(indEgoUser, 0.15f, 10, 100);
            foreach (KeyValuePair<long, double> item in recommendation)
                Console.WriteLine(item.Key + "\t" + item.Value);

            stopwatch.Stop();
            Tools.printExecutionTime(stopwatch);
            Console.WriteLine("Finished!");
        }

        // Argument #0: path of directory that stores data
        // Argument #1: the number of iterations
        // Argument #2: topN
        //static void Main(string[] args) {
        //    Stopwatch stopwatch = Stopwatch.StartNew();
        //    NetworkBuilder builder = new NetworkBuilder();
        //    builder.loadData(args[0]);

        //    List<long> egoUsers = builder.getEgoUserList();
        //    foreach (long egoUserId in egoUsers) {
        //        Console.WriteLine("Ego-network(" + egoUserId + ")");
        //        var network = builder.buildBasicNetwork(egoUserId);
        //        Dictionary<long, NodeInfo> userNodes = network.Key;
        //        Dictionary<long, NodeInfo> itemNodes = network.Value;

        //        printStatistics(userNodes.Values.ToList(), itemNodes.Values.ToList());

        //        Console.WriteLine("Making recommendation...");
        //        Recommender rec = new Recommender(userNodes, itemNodes);
        //        List<NodeInfo> recommendation = rec.Recommendation(egoUserId, 0.15f, int.Parse(args[1]));
        //        Console.WriteLine("\t* Total # of recommendation: " + recommendation.Count);

        //        Console.WriteLine("Top-10 result:");
        //        List<NodeInfo> topNList = getRecommendation(recommendation, int.Parse(args[2]));
        //        foreach (NodeInfo node in topNList)
        //            Console.WriteLine(node.id + "\t" + node.rank);

        //        break;
        //    }

        //    stopwatch.Stop();
        //    Tools.printExecutionTime(stopwatch);
        //    Console.WriteLine("Finished!");
        //}
    }
}
