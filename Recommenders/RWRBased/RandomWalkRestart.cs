using System;
using System.Collections.Generic;
using System.Linq;

namespace Recommenders.RWRBased {
    public class Node {
        public long id;
        public NodeType type;
        public double rank;
        public double newRank;
        public Dictionary<Node, double> forwardLinks;

        public Node(long id, NodeType type) {
            this.id = id;
            this.type = type;
            this.rank = 0;
            this.newRank = 0;
            this.forwardLinks = null;
        }

        public void AddRank(double value) {
            newRank += value;
        }

        public void deliverRank(Dictionary<Node, double> restart, float dampingFactor) {
            if (forwardLinks == null || forwardLinks.Count == 0) {
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
                    double weight = (entry.Value == 0) ? (1d / forwardLinks.Count) : entry.Value;
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

    public class RandomWalkRestart {
        // Node and its weight(0-1) for restart
        private Dictionary<Node, double> restart = new Dictionary<Node, double>();
        private float dampingFactor;

        // Standard Random Walk with Restart
        public RandomWalkRestart(List<Node> nodes, float dampingFactor) {
            this.dampingFactor = dampingFactor;
            foreach (Node node in nodes) {
                // Give initial and identical ranks to all nodes
                node.rank = 1d / nodes.Count;

                // Make restart vector: all the probabilities of random jump are equal for every nodes.
                restart[node] = 1d / nodes.Count;
            }
        }

        // Personalized Random Walk with Restart
        public RandomWalkRestart(List<Node> nodes, float dampingFactor, Node target) {
            this.dampingFactor = dampingFactor;
            foreach (Node node in nodes) {
                // Give initial and identical ranks to all nodes
                node.rank = 1d / nodes.Count;

                // Make restart vector: the probability of random jump exists only on the target node.
                restart[node] = (node == target) ? 1d : 0d;
            }
        }

        // Run the algorithm until convergence
        public void run() {
            double threshold = (1 / double.MaxValue) * restart.Count;
            run(threshold);
        }

        public void run(double threshold) {
            int i = 0;
            while (true) {
                Console.WriteLine(i++);
                foreach (Node node in restart.Keys)
                    node.deliverRank(restart, dampingFactor);
                if (isConverged(threshold)) {
                    // Update ranks
                    foreach (Node node in restart.Keys)
                        node.updateRank();
                    break;
                } else {
                    // Update ranks
                    foreach (Node node in restart.Keys)
                        node.updateRank();
                }
            }
        }

        // Run iterative algorithm as many as the given number
        public void run(int nIterations) {
            for (int i = 0; i < nIterations; i++) {
                Console.WriteLine(i);
                foreach (Node node in restart.Keys)
                    node.deliverRank(restart, dampingFactor);
                foreach (Node node in restart.Keys)
                    node.updateRank();
            }
        }

        // Check if the total amount of each node's rank variation is less than threshold (at the next step)
        private bool isConverged(double threshold) {
            double diffs = restart.Sum(node => Math.Abs(node.Key.newRank - node.Key.rank));
            return diffs < threshold ? true : false;
        }
    }
}
