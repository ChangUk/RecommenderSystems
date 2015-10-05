﻿using Recommenders.RWRBased;
using System;
using System.Collections.Generic;
using System.IO;

namespace TweetRecommender {
    public class DataLoader {
        // Ego user's ID
        private long egoUserId;

        // Database adapter
        private SQLiteAdapter dbAdapter;

        // Graph information
        private int nNodes = 0;
        private int nLinks = 0;
        public Dictionary<int, Node> allNodes = new Dictionary<int, Node>();
        public Dictionary<int, List<ForwardLink>> allLinks = new Dictionary<int, List<ForwardLink>>();

        // Necessary for checking node dulpication
        public Dictionary<long, int> userIDs = new Dictionary<long, int>();
        public Dictionary<long, int> memberIDs = new Dictionary<long, int>();
        public Dictionary<long, int> tweetIDs = new Dictionary<long, int>();

        // K-Fold Cross Validation
        private int nFolds;
        public HashSet<long> testSet;

        public DataLoader(string dbPath, int nFolds) {
            this.egoUserId = long.Parse(Path.GetFileNameWithoutExtension(dbPath));
            this.dbAdapter = new SQLiteAdapter(dbPath);
            this.nFolds = nFolds;

            // Add ego user's node
            addUserNode(egoUserId, NodeType.USER);
        }

        public void addUserNode(long id, NodeType type) {
            if (!userIDs.ContainsKey(id)) {
                Node node = new Node(id, type);
                allNodes.Add(nNodes, node);
                userIDs.Add(id, nNodes);
                if (type == NodeType.USER)
                    memberIDs.Add(id, nNodes);
                nNodes += 1;
            }
        }

        public void addTweetNode(long id, NodeType type) {
            if (!tweetIDs.ContainsKey(id)) {
                Node node = new Node(id, type);
                allNodes.Add(nNodes, node);
                tweetIDs.Add(id, nNodes);
                nNodes += 1;
            }
        }

        public void addLink(int idxSourceNode, int idxTargetNode, EdgeType type, double weight) {
            if (!allLinks.ContainsKey(idxSourceNode))
                allLinks.Add(idxSourceNode, new List<ForwardLink>());
            ForwardLink link = new ForwardLink(idxTargetNode, type, weight);
            if (!allLinks[idxSourceNode].Contains(link)) {
                allLinks[idxSourceNode].Add(link);
                nLinks += 1;
            }
        }

        public int getLikeCount(int idxNode) {
            int nLikes = 0;
            foreach (ForwardLink link in allLinks[idxNode]) {
                if (link.type == EdgeType.LIKE)
                    nLikes += 1;
            }
            return nLikes;
        }

        public KeyValuePair<HashSet<long>, HashSet<long>> spliteLikeHistory(HashSet<long> likes, int fold) {
            List<long> likesList = new List<long>();
            foreach (long like in likes)
                likesList.Add(like);
            likesList.Sort();

            HashSet<long> trainSet = new HashSet<long>();
            HashSet<long> testSet = new HashSet<long>();
            int unitSize = likes.Count / nFolds;
            int idxLowerbound = unitSize * fold;
            int idxUpperbound = (fold < nFolds - 1) ? unitSize * (fold + 1) : likes.Count;
            for (int idx = 0; idx < likesList.Count; idx++) {
                if (idxLowerbound <= idx && idx < idxUpperbound)
                    trainSet.Add(likesList[idx]);
                else
                    testSet.Add(likesList[idx]);
            }
            return new KeyValuePair<HashSet<long>, HashSet<long>>(trainSet, testSet);
        }

        public void graphConfiguration(RecSys type, int fold) {
            if (type == RecSys.BASELINE)
                graphConfiguration_baseline(fold);
            else if (type == RecSys.PROPOSED1)
                graphConfiguration_proposed1();
        }

        /// <summary>
        /// Baseline method
        /// <para>No user friendship</para>
        /// </summary>
        public void graphConfiguration_baseline(int fold) {
            Console.WriteLine("Graph(" + egoUserId + " - baseline) Configuration... Fold #" + fold);

            // Get members of ego network
            HashSet<long> followeesOfEgoUser = dbAdapter.getFollowingUsers(egoUserId);
            foreach (long followee in followeesOfEgoUser) {
                HashSet<long> followeesOfFollowee = dbAdapter.getFollowingUsers(followee);
                if (followeesOfFollowee.Contains(egoUserId))
                    addUserNode(followee, NodeType.USER);
            }

            // Tweets that members like: retweet, quote, favorite
            foreach (KeyValuePair<long, int> entry in memberIDs) {
                long memberId = entry.Key;
                int idxMember = entry.Value;

                // Tweet IDs a member likes
                HashSet<long> likes = new HashSet<long>();
                HashSet<long> retweets = dbAdapter.getRetweets(memberId);
                foreach (long retweet in retweets)
                    likes.Add(retweet);
                HashSet<long> quotes = dbAdapter.getQuotedTweets(memberId);
                foreach (long quote in quotes)
                    likes.Add(quote);
                HashSet<long> favorites = dbAdapter.getFavoriteTweets(memberId);
                foreach (long favorite in favorites)
                    likes.Add(favorite);

                // If the user is ego user, his like history is divided into training set and test set.
                if (idxMember == 0) {
                    // The number of tweets the ego user likes should be more than # of folds.
                    if (likes.Count < nFolds) {
                        Console.WriteLine("The number of like history is less than nFolds.");
                        Console.WriteLine("\t* # of likes: " + likes.Count);
                        Console.WriteLine("\t* # of folds: " + nFolds);
                        break;
                    }

                    // Split ego user's like history into two
                    var data = spliteLikeHistory(likes, fold);
                    foreach (long like in data.Key) {               // Likes except test tweets
                        addTweetNode(like, NodeType.ITEM);
                        int idxTweet = tweetIDs[like];
                        addLink(idxMember, idxTweet, EdgeType.LIKE, 1);
                        addLink(idxTweet, idxMember, EdgeType.LIKE, 1);
                    }

                    // Set test set
                    testSet = data.Value;
                } else {
                    foreach (long like in likes) {
                        addTweetNode(like, NodeType.ITEM);
                        int idxTweet = tweetIDs[like];
                        addLink(idxMember, idxTweet, EdgeType.LIKE, 1);
                        addLink(idxTweet, idxMember, EdgeType.LIKE, 1);
                    }
                }
            }

            // Print out the graph information
            Console.WriteLine("# of nodes: " + nNodes);
            Console.WriteLine("\t* User: " + userIDs.Count);
            Console.WriteLine("\t* Tweet: " + tweetIDs.Count);
            Console.WriteLine("# of links: " + nLinks);
        }

        /// <summary>
        /// Propoased method #1
        /// <para>Include friendship</para>
        /// </summary>
        public void graphConfiguration_proposed1() {

        }

        /// <summary>
        /// Propoased method #2
        /// <para>Include both friend nodes and unfriend nodes</para>
        /// </summary>
        public void graphConfiguration_proposed2() {

        }
    }
}
