using System.Collections.Generic;
using System.Linq;

namespace Recommenders.RWRBased {
    public class Recommender_ {
        private Dictionary<long, NodeInfo> userNodes;
        private Dictionary<long, NodeInfo> itemNodes;

        public Recommender_(Dictionary<long, NodeInfo> userNodes, Dictionary<long, NodeInfo> itemNodes) {
            this.userNodes = userNodes;
            this.itemNodes = itemNodes;
        }

        public List<NodeInfo> getItemList(long userId) {
            List<NodeInfo> itemList = new List<NodeInfo>();
            NodeInfo userNode = userNodes[userId];
            foreach (NodeInfo item in userNode.forwardLinks.Keys.ToList()) {
                if (item.type == NodeType.ITEM)
                    itemList.Add(item);
            }
            return itemList;
        }

        public List<NodeInfo> getCandidateItems(long targetUserId) {
            // Find target user's item list
            List<NodeInfo> itemListOfTargetUser = getItemList(targetUserId);

            // Find candidate items to be recommended to target user
            List<NodeInfo> candidateItems = new List<NodeInfo>();
            foreach (NodeInfo item in itemNodes.Values.ToList()) {
                if (!itemListOfTargetUser.Contains(item))
                    candidateItems.Add(item);
            }
            return candidateItems;
        }

        public List<NodeInfo> Recommendation(long targetUserId, float dampingFactor, int nIterations) {
            List<NodeInfo> userNodeList = userNodes.Values.ToList();
            List<NodeInfo> itemNodeList = itemNodes.Values.ToList();
            List<NodeInfo> allNodes = userNodeList.Concat(itemNodeList).ToList();

            // Run Personalized Random Walk with Restart algorithm
            NodeInfo targetNode = userNodes[targetUserId];
            RandomWalkRestart rwr = new RandomWalkRestart(allNodes, dampingFactor, targetNode);
            rwr.run(nIterations);

            // Make recommendation by sorting candidate item list by rank
            List<NodeInfo> candidateItems = getCandidateItems(targetUserId);
            candidateItems.Sort((x, y) => { return x.rank.CompareTo(y.rank); });
            candidateItems.Reverse();

            // Discard items whose rank is zero
            List<NodeInfo> recommendation = new List<NodeInfo>();
            foreach (NodeInfo item in candidateItems) {
                if (item.rank > 0)
                    recommendation.Add(item);
            }
            return recommendation;
        }
    }
}
