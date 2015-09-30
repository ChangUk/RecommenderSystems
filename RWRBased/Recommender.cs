using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RWRBased {
    public enum NodeType { USER, ITEM }

    public class Recommender {
        private Dictionary<string, Node> nodes;

        public Recommender() {
            this.nodes = new Dictionary<string, Node>();
        }

        public void loadData(string filePath) {
            StreamReader file = new StreamReader(filePath);
            string line;
            char[] delimiterChars = { '\t' };
            while ((line = file.ReadLine()) != null) {
                string[] tokens = line.Split(delimiterChars);
                Dictionary<Node, double> links = new Dictionary<Node, double>();
                for (int i = 0; i < tokens.Length; i++) {
                    // Get node by key string
                    Node node;
                    if (!nodes.TryGetValue(tokens[i], out node)) {
                        node = new Node(tokens[i], NodeType.USER);
                        nodes.Add(tokens[i], node);
                    }

                    // Add following nodes into forward links with their weights
                    if (i > 0)
                        links.Add(node, 1.0d / (tokens.Length - 1));
                }

                // Set target node's forward links
                nodes[tokens[0]].forwardLinks = links;
            }
            file.Close();
        }

        public List<KeyValuePair<string, double>> Recommendation(string targetUserId) {
            // Give rank score to all nodes
            foreach (KeyValuePair<string, Node> entry in nodes)
                entry.Value.rank = 1.0d / nodes.Count;

            // Run Personalized PageRank algorithm
            Node targetUser = nodes[targetUserId];
            PageRank pagerank = new PageRank(nodes.Values.ToList(), 0.15f, targetUser);
            double threshold = (1 / double.MaxValue) * nodes.Count;
            pagerank.run(threshold);

            // Sort by rank
            List<KeyValuePair<string, Node>> nodeList = nodes.ToList();
            nodeList.Sort((x, y) => { return x.Value.rank.CompareTo(y.Value.rank); });
            nodeList.Reverse();

            // Make recommendation
            List<KeyValuePair<string, double>> recommendation = new List<KeyValuePair<string, double>>();
            foreach (KeyValuePair<string, Node> entry in nodeList)
                recommendation.Add(new KeyValuePair<string, double>(entry.Key, entry.Value.rank));
            return recommendation;
        }
    }
}
