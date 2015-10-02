using Recommenders.RWRBased;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace TweetRecommender {
    public class Program {
        private static void printStatistics(List<Node> userNodeList, List<Node> itemNodeList) {
            int cntNoForwardLinks = 0;

            Console.WriteLine("\t* # of users: " + userNodeList.Count);
            Console.WriteLine("\t* # of tweets: " + itemNodeList.Count);

            foreach (Node node in userNodeList) {
                if (node.forwardLinks == null)
                    cntNoForwardLinks += 1;
            }
            Console.WriteLine("\t* # of users having no like history: " + cntNoForwardLinks);
            cntNoForwardLinks = 0;
            foreach (Node node in itemNodeList)
                if (node.forwardLinks == null)
                    cntNoForwardLinks += 1;
            Console.WriteLine("\t* # of tweets having no like history: " + cntNoForwardLinks);
        }

        private static List<Node> getRecommendation(List<Node> fullItemList, int topN) {
            List<Node> topNRecommendation = new List<Node>();
            for (int i = 0; i < topN; i++)
                topNRecommendation.Add(fullItemList[i]);
            return topNRecommendation;
        }
        
        // Argument #0: path of directory that stores data
        // Argument #1: the number of iterations
        // Argument #2: topN
        static void Main(string[] args) {
            Stopwatch stopwatch = Stopwatch.StartNew();
            NetworkBuilder builder = new NetworkBuilder(args[0]);

            List<long> egoUsers = builder.getEgoUserList();
            foreach (long egoUserId in egoUsers) {
                Console.WriteLine("Ego-network(" + egoUserId + ")");
                var network = builder.buildBasicNetwork(egoUserId);
                Dictionary<long, Node> userNodes = network.Key;
                Dictionary<long, Node> itemNodes = network.Value;

                printStatistics(userNodes.Values.ToList(), itemNodes.Values.ToList());

                Console.WriteLine("Making recommendation...");
                Recommender rec = new Recommender(userNodes, itemNodes);
                List<Node> recommendation = rec.Recommendation(egoUserId, 0.15f, int.Parse(args[1]));
                Console.WriteLine("\t* Total # of recommendation: " + recommendation.Count);

                Console.WriteLine("Top-10 result:");
                List<Node> topNList = getRecommendation(recommendation, int.Parse(args[2]));
                foreach (Node node in topNList)
                    Console.WriteLine(node.id + "\t" + node.rank);

                break;
            }
            
            stopwatch.Stop();
            var timespan = TimeSpan.FromMilliseconds(stopwatch.ElapsedMilliseconds);
            Console.WriteLine("Finished! - Execution time: " + timespan.ToString());
        }
    }
}
