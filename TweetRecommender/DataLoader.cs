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
        private int indNode;
        public Dictionary<int, Node> allNodes;
        public Dictionary<int, List<ForwardLink>> allLinks;

        // Necessary for checking node dulpication
        public Dictionary<long, int> userIDs;
        public Dictionary<long, int> memberIDs;      // Users who are linked with friendship only
        public Dictionary<long, int> tweetIDs;
        
        public DataLoader(string dbPath, long egoUserId) {
            // Add ego user node
            this.egoUserId = egoUserId;
            addUserNode(egoUserId, NodeType.USER);

            // Database adapter
            this.dbAdapter = new SQLiteAdapter(dbPath);
        }

        public void addUserNode(long id, NodeType type) {
            if (!userIDs.ContainsKey(id)) {
                Node node = new Node(id, type);
                allNodes.Add(indNode, node);
                userIDs.Add(id, indNode);
                if (type == NodeType.USER)
                    memberIDs.Add(id, indNode);
                indNode += 1;
            }
        }

        public void addTweetNode(long id, NodeType type) {
            if (!tweetIDs.ContainsKey(id)) {
                Node node = new Node(id, type);
                allNodes.Add(indNode, node);
                tweetIDs.Add(id, indNode);
                indNode += 1;
            }
        }

        public void addLink(int indSource, int indTarget, EdgeType type, double weight) {
            if (!allLinks.ContainsKey(indSource))
                allLinks.Add(indSource, new List<ForwardLink>());
            ForwardLink link = new ForwardLink(indTarget, type, weight);
            if (!allLinks[indSource].Contains(link))
                allLinks[indSource].Add(link);
        }

        /// <summary>
        /// Baseline method
        /// <para>No user friendship</para>
        /// </summary>
        public void graphConfiguration_baseline() {
            // Initialize graph information
            indNode = 0;
            allNodes = new Dictionary<int, Node>();
            allLinks = new Dictionary<int, List<ForwardLink>>();
            userIDs = new Dictionary<long, int>();
            memberIDs = new Dictionary<long, int>();
            tweetIDs = new Dictionary<long, int>();

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
                int indMember = entry.Value;

                // Retweet
                HashSet<long> retweets = dbAdapter.getRetweets(memberId);
                foreach (long retweet in retweets) {
                    addTweetNode(retweet, NodeType.ITEM);
                    int indTweet = tweetIDs[retweet];

                    // Add link
                    addLink(indMember, indTweet, EdgeType.LIKE, 1);
                    addLink(indTweet, indMember, EdgeType.LIKE, 1);
                }

                // Quote
                HashSet<long> quotes = dbAdapter.getQuotedTweets(memberId);
                foreach (long retweet in retweets) {
                    addTweetNode(retweet, NodeType.ITEM);
                    int indTweet = tweetIDs[retweet];

                    // Add link
                    addLink(indMember, indTweet, EdgeType.LIKE, 1);
                    addLink(indTweet, indMember, EdgeType.LIKE, 1);
                }

                // Favorite
                HashSet<long> favorites = dbAdapter.getFavoriteTweets(memberId);
                foreach (long retweet in retweets) {
                    addTweetNode(retweet, NodeType.ITEM);
                    int indTweet = tweetIDs[retweet];

                    // Add link
                    addLink(indMember, indTweet, EdgeType.LIKE, 1);
                    addLink(indTweet, indMember, EdgeType.LIKE, 1);
                }
            }
        }

        /// <summary>
        /// Propoased method #1
        /// <para>Include friendship</para>
        /// </summary>
        public void graphConfiguration_proposed1() {
            // Initialize graph information
            indNode = 0;
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
            indNode = 0;
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
