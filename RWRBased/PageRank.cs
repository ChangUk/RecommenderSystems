using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RWRBased {
    public class Node {
        public string id;
        public NodeType type;
        public double rank;
        public double newRank;
        public Dictionary<Node, double> forwardLinks;

        public Node(string id, NodeType type)
            : this(id, type, 0, new Dictionary<Node, double>()) {
        }

        public Node(string id, NodeType type, double initRank)
            : this(id, type, initRank, new Dictionary<Node, double>()) {
        }

        public Node(string id, NodeType type, double initRank, Dictionary<Node, double> forwardLinks) {
            this.id = id;
            this.type = type;
            this.rank = initRank;
            this.newRank = 0;
            this.forwardLinks = forwardLinks;
        }

        public void AddRank(double value) {
            newRank += value;
        }

        public void deliverRank(Dictionary<Node, double> restart, float dampingFactor) {
            if (forwardLinks.Count == 0) {
                // Dangling node
                foreach (KeyValuePair<Node, double> entry in restart) {
                    Node node = entry.Key;
                    double weight = entry.Value;
                    node.AddRank(rank * weight);
                }
            } else {
                double rank_randomWalk = (1 - dampingFactor) * rank;
                foreach (KeyValuePair<Node, double> entry in forwardLinks) {
                    Node node = entry.Key;
                    double weight = entry.Value;
                    node.AddRank(rank_randomWalk * weight);
                }
                double rank_restart = rank - rank_randomWalk;
                foreach (KeyValuePair<Node, double> entry in restart) {
                    Node node = entry.Key;
                    double weight = entry.Value;
                    node.AddRank(rank_restart * weight);
                }
            }
        }

        public void updateRank() {
            rank = newRank;
            newRank = 0d;
        }
    }

    class PageRank {
        // Node and its weight(0-1) for restart
        private Dictionary<Node, double> nodes;
        private float dampingFactor;

        public PageRank(List<Node> nodes, float dampingFactor) {
            this.nodes = new Dictionary<Node, double>();
            this.dampingFactor = dampingFactor;
            foreach (Node node in nodes) 
                this.nodes.Add(node, 1d / nodes.Count);
        }

        // Personalized PageRank
        public PageRank(List<Node> nodes, float dampingFactor, Node target) {
            this.nodes = new Dictionary<Node, double>();
            this.dampingFactor = dampingFactor;
            foreach (Node node in nodes)
                this.nodes.Add(node, (node == target ? 1d : 0d));
        }

        // Run PageRank algorithm until convergence
        public void run(double threshold) {
            while (true) {
                foreach (Node node in nodes.Keys)
                    node.deliverRank(nodes, dampingFactor);
                if (isConverged(threshold)) {
                    foreach (Node node in nodes.Keys)
                        node.updateRank();
                    break;
                } else {
                    foreach (Node node in nodes.Keys)
                        node.updateRank();
                }
            }
        }

        // Check if the amount of each node's rank variation is less than threshold
        private bool isConverged(double threshold) {
            double diffs = nodes.Sum(node => Math.Abs(node.Key.newRank - node.Key.rank));
            return diffs < threshold ? true : false;
        }
    }
}
