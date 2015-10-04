using Recommenders;
using Recommenders.RWRBased;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace TweetRecommender {
    public class DataLoader {
        // Ego user's ID
        private long egoUserId;

        // Database adapter
        private SQLiteAdapter dbAdapter;

        // Graph information
        private int idxNode;
        public Dictionary<int, Node> allNodes;
        public Dictionary<int, List<ForwardLink>> allLinks;

        // Necessary for checking node dulpication
        public Dictionary<long, int> userIDs;
        public Dictionary<long, int> memberIDs;      // Users who are linked with friendship only
        public Dictionary<long, int> tweetIDs;
        
        public DataLoader(string dbPath, long egoUserId) {
            this.egoUserId = egoUserId;
            this.dbAdapter = new SQLiteAdapter(dbPath);
        }

        public void addUserNode(long id, NodeType type) {
            if (!userIDs.ContainsKey(id)) {
                Node node = new Node(id, type);
                allNodes.Add(idxNode, node);
                userIDs.Add(id, idxNode);
                if (type == NodeType.USER)
                    memberIDs.Add(id, idxNode);
                idxNode += 1;
            }
        }

        public void addTweetNode(long id, NodeType type) {
            if (!tweetIDs.ContainsKey(id)) {
                Node node = new Node(id, type);
                allNodes.Add(idxNode, node);
                tweetIDs.Add(id, idxNode);
                idxNode += 1;
            }
        }

        public void addLink(int idxSourceNode, int idxTargetNode, EdgeType type, double weight) {
            if (!allLinks.ContainsKey(idxSourceNode))
                allLinks.Add(idxSourceNode, new List<ForwardLink>());
            ForwardLink link = new ForwardLink(idxTargetNode, type, weight);
            if (!allLinks[idxSourceNode].Contains(link))
                allLinks[idxSourceNode].Add(link);
        }

        /// <summary>
        /// Baseline method
        /// <para>No user friendship</para>
        /// </summary>
        public void graphConfiguration_baseline() {
            // Initialize graph information
            idxNode = 0;
            allNodes = new Dictionary<int, Node>();
            allLinks = new Dictionary<int, List<ForwardLink>>();
            userIDs = new Dictionary<long, int>();
            memberIDs = new Dictionary<long, int>();
            tweetIDs = new Dictionary<long, int>();

            // Add ego user's node
            addUserNode(egoUserId, NodeType.USER);

            // Followees of ego user
            HashSet<long> followeesOfEgoUser = dbAdapter.getFollowingUsers(egoUserId);

            // Members of ego network
            foreach (long followee in followeesOfEgoUser) {
                HashSet<long> followeesOfFollowee = dbAdapter.getFollowingUsers(followee);
                if (followeesOfFollowee.Contains(egoUserId))
                    addUserNode(followee, NodeType.USER);
            }

            // Tweets that members like: retweet, quote, favorite
            foreach (KeyValuePair<long, int> entry in memberIDs) {
                long memberId = entry.Key;
                int idxMember = entry.Value;

                // Retweet
                HashSet<long> retweets = dbAdapter.getRetweets(memberId);
                foreach (long retweet in retweets) {
                    addTweetNode(retweet, NodeType.ITEM);
                    int idxTweet = tweetIDs[retweet];

                    // Add link
                    addLink(idxMember, idxTweet, EdgeType.LIKE, 1);
                    addLink(idxTweet, idxMember, EdgeType.LIKE, 1);
                }

                // Quote
                HashSet<long> quotes = dbAdapter.getQuotedTweets(memberId);
                foreach (long quote in quotes) {
                    addTweetNode(quote, NodeType.ITEM);
                    int idxTweet = tweetIDs[quote];

                    // Add link
                    addLink(idxMember, idxTweet, EdgeType.LIKE, 1);
                    addLink(idxTweet, idxMember, EdgeType.LIKE, 1);
                }

                // Favorite
                HashSet<long> favorites = dbAdapter.getFavoriteTweets(memberId);
                foreach (long favorite in favorites) {
                    addTweetNode(favorite, NodeType.ITEM);
                    int idxTweet = tweetIDs[favorite];

                    // Add link
                    addLink(idxMember, idxTweet, EdgeType.LIKE, 1);
                    addLink(idxTweet, idxMember, EdgeType.LIKE, 1);
                }
            }

            Console.WriteLine("# of usernodes: " + userIDs.Count);
            Console.WriteLine("# of tweetnodes: " + tweetIDs.Count);
            Console.WriteLine("# of nodes: " + allNodes.Count);
            int cnt = 0;
            foreach (List<ForwardLink> links in allLinks.Values)
                cnt += links.Count;
            Console.WriteLine("# of links: " + cnt);
        }

        /// <summary>
        /// Propoased method #1
        /// <para>Include friendship</para>
        /// </summary>
        public void graphConfiguration_proposed1() {
            // Initialize graph information
            idxNode = 0;
            allNodes = new Dictionary<int, Node>();
            allLinks = new Dictionary<int, List<ForwardLink>>();
            userIDs = new Dictionary<long, int>();
            memberIDs = new Dictionary<long, int>();
            tweetIDs = new Dictionary<long, int>();

            // TODO: proposed method
            HashSet<long> followeesOfEgoUser = dbAdapter.getFollowingUsers(egoUserId);
            foreach (long followee in followeesOfEgoUser) {
                HashSet<long> followeesOfFollowee = dbAdapter.getFollowingUsers(followee);
                if (followeesOfFollowee.Contains(egoUserId)) {
                    // Co-followship
                    addUserNode(followee, NodeType.USER);

                    // Add followship links (undirected)

                }
            }
        }

        /// <summary>
        /// Propoased method #2
        /// <para>Include both friend nodes and unfriend nodes</para>
        /// </summary>
        public void graphConfiguration_proposed2() {
            // Initialize graph information
            idxNode = 0;
            allNodes = new Dictionary<int, Node>();
            allLinks = new Dictionary<int, List<ForwardLink>>();
            userIDs = new Dictionary<long, int>();
            memberIDs = new Dictionary<long, int>();
            tweetIDs = new Dictionary<long, int>();

            // TODO: proposed method
            HashSet<long> followeesOfEgoUser = dbAdapter.getFollowingUsers(egoUserId);
            foreach (long followee in followeesOfEgoUser) {
                HashSet<long> followeesOfFollowee = dbAdapter.getFollowingUsers(followee);
                if (followeesOfFollowee.Contains(egoUserId)) {
                    // Co-followship
                    addUserNode(followee, NodeType.USER);

                    // Add followship links (undirected)

                } else {
                    // OneWay-followship
                    addUserNode(followee, NodeType.ETC);
                }
            }
        }
    }
}
