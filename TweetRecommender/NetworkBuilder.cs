using Recommenders.RWRBased;
using System.Collections.Generic;

namespace TweetRecommender {
    public class NetworkBuilder {
        private Data data;

        public NetworkBuilder(string pathData) {
            data = new Data();
            data.loadData(pathData);
        }

        public List<long> getEgoUserList() {
            return data.egoUsers;
        }

        public KeyValuePair<Dictionary<long, Node>, Dictionary<long, Node>> buildBasicNetwork(long egoUserId) {
            Dictionary<long, Node> userNodes = new Dictionary<long, Node>();
            Dictionary<long, Node> itemNodes = new Dictionary<long, Node>();

            // Make user nodes
            Node seedNode = new Node(egoUserId, NodeType.USER);
            userNodes.Add(egoUserId, seedNode);
            foreach (long friendId in data.friends[egoUserId]) {
                if (!userNodes.ContainsKey(friendId)) {
                    Node friendNode = new Node(friendId, NodeType.USER);
                    userNodes.Add(friendId, friendNode);
                }
            }

            // Make tweet nodes
            foreach (long userId in userNodes.Keys) {
                foreach (long tweetId in data.likes[userId]) {
                    if (!itemNodes.ContainsKey(tweetId)) {
                        Node tweetNode = new Node(tweetId, NodeType.ITEM);
                        itemNodes.Add(tweetId, tweetNode);
                    }
                }
            }

            // Make links that a user likes a tweet
            foreach (long userId in userNodes.Keys) {
                foreach (long tweetId in data.likes[userId]) {
                    Node userNode = userNodes[userId];
                    Node itemNode = itemNodes[tweetId];
                    if (userNode.forwardLinks == null)
                        userNode.forwardLinks = new Dictionary<Node, double>();
                    if (itemNode.forwardLinks == null)
                        itemNode.forwardLinks = new Dictionary<Node, double>();
                    userNode.forwardLinks.Add(itemNode, 0);
                    itemNode.forwardLinks.Add(userNode, 0);
                }
            }

            return new KeyValuePair<Dictionary<long, Node>, Dictionary<long, Node>>(userNodes, itemNodes);
        }
    }
}
