using System;
using System.Collections.Generic;

namespace Recommenders.RWRBased {
    public struct ForwardLink {
        public int targetNode;
        public EdgeType type;
        public double weight;

        public ForwardLink(int targetNode, EdgeType type, double weight) {
            this.targetNode = targetNode;
            this.type = type;
            this.weight = weight;
        }
    };

    public class Graph {
        // Weighted and directed graph
        public Dictionary<int, ForwardLink[]> graph;

        // The list of all nodes existing in the graph
        public HashSet<int> nodeList;

        public Graph() {
            this.graph = new Dictionary<int, ForwardLink[]>();
            this.nodeList = new HashSet<int>();
        }

        public void buildGraph(Dictionary<int, List<ForwardLink>> links) {
            foreach (KeyValuePair<int, List<ForwardLink>> entry in links) {
                // Add source node
                nodeList.Add(entry.Key);
                
                ForwardLink[] forwardLinks = null;
                if (entry.Value != null && entry.Value.Count > 0) {
                    int nLinks = entry.Value.Count;

                    // Make an array space for forward links
                    forwardLinks = new ForwardLink[nLinks];

                    int position = 0;
                    double sumWeights = 0;
                    foreach (ForwardLink link in entry.Value) {
                        forwardLinks[position++] = link;
                        sumWeights += link.weight;

                        // Add target node
                        nodeList.Add(link.targetNode);
                    }

                    // Adjust weights whose sum is 1
                    for (int i = 0; i < nLinks; i++)
                        forwardLinks[i].weight /= sumWeights;
                }
                
                // Add forward links of the source node
                graph.Add(entry.Key, forwardLinks);
            }
        }

        // Get the number of nodes in the graph
        public int size() {
            return nodeList.Count;
        }
    }
}
