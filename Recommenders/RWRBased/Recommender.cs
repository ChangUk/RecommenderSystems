﻿using System.Collections.Generic;
using System.Linq;

namespace Recommenders.RWRBased {
    public enum NodeType { USER, ITEM }
    public enum EdgeType { LIKE, FRIENDSHIP, COMMUNICATION, ATHORSHIP, PURCHASE, ETC }

    public class Recommender {
        private Dictionary<long, Node> userNodes;
        private Dictionary<long, Node> itemNodes;

        public Recommender(Dictionary<long, Node> userNodes, Dictionary<long, Node> itemNodes) {
            this.userNodes = userNodes;
            this.itemNodes = itemNodes;
        }

        public List<Node> getItemList(long userId) {
            List<Node> itemList = new List<Node>();
            Node userNode = userNodes[userId];
            foreach (Node item in userNode.forwardLinks.Keys.ToList()) {
                if (item.type == NodeType.ITEM)
                    itemList.Add(item);
            }
            return itemList;
        }

        public List<Node> getCandidateItems(long targetUserId) {
            // Find target user's item list
            List<Node> itemListOfTargetUser = getItemList(targetUserId);

            // Find candidate items to be recommended to target user
            List<Node> candidateItems = new List<Node>();
            foreach (Node item in itemNodes.Values.ToList()) {
                if (!itemListOfTargetUser.Contains(item))
                    candidateItems.Add(item);
            }
            return candidateItems;
        }

        public List<Node> Recommendation(long targetUserId, float dampingFactor, int nIterations) {
            List<Node> userNodeList = userNodes.Values.ToList();
            List<Node> itemNodeList = itemNodes.Values.ToList();
            List<Node> allNodes = userNodeList.Concat(itemNodeList).ToList();

            // Run Personalized Random Walk with Restart algorithm
            Node targetNode = userNodes[targetUserId];
            RandomWalkRestart rwr = new RandomWalkRestart(allNodes, dampingFactor, targetNode);
            rwr.run(nIterations);

            // Make recommendation by sorting candidate item list by rank
            List<Node> candidateItems = getCandidateItems(targetUserId);
            candidateItems.Sort((x, y) => { return x.rank.CompareTo(y.rank); });
            candidateItems.Reverse();

            // Discard items whose rank is zero
            List<Node> recommendation = new List<Node>();
            foreach (Node item in candidateItems) {
                if (item.rank > 0)
                    recommendation.Add(item);
            }
            return recommendation;
        }
    }
}
