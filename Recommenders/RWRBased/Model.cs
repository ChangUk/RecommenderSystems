﻿using System;
using System.Collections.Generic;

namespace Recommenders.RWRBased {
    public class Model {
        public Graph graph;
        public double[] rank;
        public double[] nextRank;
        public int nNodes;

        public double dampingFactor;
        public double[] restart;

        public Model(Graph graph, double dampingFactor) {
            this.graph = graph;
            this.nNodes = graph.size();
            this.dampingFactor = dampingFactor;

            rank = new double[nNodes];
            nextRank = new double[nNodes];
            restart = new double[nNodes];

            for (int i = 0; i < nNodes; i++) {
                // Initialize rank score of each node
                rank[i] = 1d / nNodes;
                nextRank[i] = 0;

                // Make restart weight
                restart[i] = 1d / nNodes;
            }
        }

        public Model(Graph graph, double dampingFactor, int targetNode) {
            this.graph = graph;
            this.nNodes = graph.size();
            this.dampingFactor = dampingFactor;
            
            rank = new double[nNodes];
            nextRank = new double[nNodes];
            restart = new double[nNodes];

            for (int i = 0; i < nNodes; i++) {
                // Initialize rank score of each node
                rank[i] = (i == targetNode) ? 1 : 0;
                nextRank[i] = 0;

                // Make restart weight
                restart[i] = (i == targetNode) ? 1 : 0;
            }
        }

        public void run(int nIterations) {
            for (int n = 0; n < nIterations; n++) {
                // Print out the number of iterations so far
                Console.WriteLine("Iteration: " + (n + 1));
                
                // Deliever and update ranks
                deliverRanks();
                updateRanks();
            }
        }

        public void run() {
            double threshold = (1 / double.MaxValue) * graph.size();
            run(threshold);
        }

        public void run(double threshold) {
            int n = 0;
            while (true) {
                // Print out the number of iterations so far
                Console.WriteLine("Iteration: " + (n + 1));

                deliverRanks();
                if (checkConvergence(threshold)) {
                    updateRanks();
                    return;
                }
                updateRanks();
            }
        }

        // Deliver ranks along with forward links
        public void deliverRanks() {
            Dictionary<int, ForwardLink[]> forwardLinks = graph.graph;
            for (int i = 0; i < nNodes; i++) {
                if (!forwardLinks.ContainsKey(i))
                    continue;
                ForwardLink[] links = forwardLinks[i];
                if (links != null && links.Length > 0) {
                    int nLinks = links.Length;

                    // Deliver rank score with Random Walk
                    double rank_randomWalk = (1 - dampingFactor) * rank[i];
                    for (int w = 0; w < nLinks; w++) {
                        ForwardLink link = links[w];
                        nextRank[link.targetNode] += rank_randomWalk * link.weight;
                    }

                    // Deliver rank score with Restart
                    double rank_restart = rank[i] - rank_randomWalk;
                    for (int r = 0; r < nNodes; r++)
                        nextRank[r] += rank_restart * restart[r];
                } else {
                    // Dangling node: the rank score is delivered along with only virtual links
                    for (int r = 0; r < nNodes; r++)
                        nextRank[r] += rank[i] * restart[r];
                }
            }
        }

        // Replace current rank score with new one
        public void updateRanks() {
            for (int i = 0; i < nNodes; i++) {
                rank[i] = nextRank[i];
                nextRank[i] = 0;
            }
        }

        public bool checkConvergence(double threshold) {
            double diff = 0;
            for (int i = 0; i < nNodes; i++)
                diff += (rank[i] > nextRank[i]) ? (rank[i] - nextRank[i]) : (nextRank[i] - rank[i]);
            return (diff < threshold) ? true : false;
        }
    }
}
