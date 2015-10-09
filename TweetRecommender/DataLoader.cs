using Recommenders.RWRBased;
using System;
using System.Collections.Generic;
using System.IO;

namespace TweetRecommender {
    public class DataLoader {
        // Ego user's ID
        private long egoUserId;
        public int nLikesOfEgoUser;

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
        public Dictionary<long, int> thirdPartyIDs = new Dictionary<long, int>();

        // K-Fold Cross Validation
        private int nFolds;
        public HashSet<long> testSet;

        public DataLoader(string dbPath, int nFolds) {
            this.egoUserId = long.Parse(Path.GetFileNameWithoutExtension(dbPath));
            this.dbAdapter = new SQLiteAdapter(dbPath);
            this.nFolds = nFolds;
        }

        // Add user node including third party users
        public void addUserNode(long id, NodeType type) {
            if (!userIDs.ContainsKey(id)) {
                Node node = new Node(id, type);
                allNodes.Add(nNodes, node);
                userIDs.Add(id, nNodes);
                if (type == NodeType.USER)
                    memberIDs.Add(id, nNodes);
                else
                    thirdPartyIDs.Add(id, nNodes);
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

        public KeyValuePair<HashSet<long>, HashSet<long>> splitLikeHistory(HashSet<long> likes, int fold) {
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
                    testSet.Add(likesList[idx]);
                else
                    trainSet.Add(likesList[idx]);
            }
            return new KeyValuePair<HashSet<long>, HashSet<long>>(trainSet, testSet);
        }

        public bool graphConfiguration(Methodology type, int fold) {
            switch (type) {
                case Methodology.BASELINE:
                    return graphConfiguration_baseline(fold);
                case Methodology.INCL_FRIENDSHIP:
                    return graphConfiguration_friendship(fold);
                case Methodology.INCL_ALLFOLLOWSHIP:
                    return graphConfiguration_allFollowship(fold);
                case Methodology.INCL_AUTHORSHIP:
                    return graphConfiguration_authorship(fold);
                default:
                    return false;
            }
        }

        /// <summary>
        /// Baseline method
        /// <para>No user friendship</para>
        /// </summary>
        public bool graphConfiguration_baseline(int fold) {
            lock (Program.locker)
                Console.WriteLine("Graph(" + egoUserId + " - 0.baseline) Configuration... Fold #" + (fold + 1) + "/" + nFolds);

            // Makeup user and tweet nodes and their relations
            addUserNodes();
            bool isValid = addTweetNodesAndLikeEdges(fold);
            if (isValid == false)
                return false;

            // Print out the graph information
            printGraphInfo();
            return true;
        }

        /// <summary>
        /// Propoased method #1
        /// <para>Include friendship relations</para>
        /// </summary>
        public bool graphConfiguration_friendship(int fold) {
            lock (Program.locker)
                Console.WriteLine("Graph(" + egoUserId + " - 1.incl_friendship) Configuration... Fold #" + (fold + 1) + "/" + nFolds);

            // Makeup user and tweet nodes and their relations
            addUserNodes();
            bool isValid = addTweetNodesAndLikeEdges(fold);
            if (isValid == false)
                return false;

            // Add friendship links among network users
            addFriendship();

            // Print out the graph information
            printGraphInfo();
            return true;
        }

        /// <summary>
        /// Propoased method #2
        /// <para>Include both friendship and followship third party users relations</para>
        /// </summary>
        public bool graphConfiguration_allFollowship(int fold) {
            lock (Program.locker)
                Console.WriteLine("Graph(" + egoUserId + " - 2.incl_allFollowship) Configuration... Fold #" + (fold + 1) + "/" + nFolds);

            // Makeup user and tweet nodes and their relations
            addUserNodes();
            bool isValid = addTweetNodesAndLikeEdges(fold);
            if (isValid == false)
                return false;

            // Add followship links not only among ego network members but also third party users
            addFriendshipAndFollowship();

            // Print out the graph information
            printGraphInfo();
            return true;
        }

        /// <summary>
        /// Propoased method #3
        /// <para>Include authorship relations</para>
        /// </summary>
        public bool graphConfiguration_authorship(int fold) {
            lock (Program.locker)
                Console.WriteLine("Graph(" + egoUserId + " - 3.incl_authorship) Configuration... Fold #" + (fold + 1) + "/" + nFolds);

            // Makeup user and tweet nodes and their relations
            addUserNodes();
            bool isValid = addTweetNodesAndLikeEdges(fold);
            if (isValid == false)
                return false;

            // Add friendship links among network users
            addFriendship();

            // Add authorship of members
            addAuthorship();

            // Print out the graph information
            printGraphInfo();
            return true;
        }

        public void addUserNodes() {
            // Add ego user's node
            addUserNode(egoUserId, NodeType.USER);

            // Get members of ego network
            HashSet<long> followeesOfEgoUser = dbAdapter.getFollowingUsers(egoUserId);
            foreach (long followee in followeesOfEgoUser) {
                HashSet<long> followeesOfFollowee = dbAdapter.getFollowingUsers(followee);
                if (followeesOfFollowee.Contains(egoUserId))
                    addUserNode(followee, NodeType.USER);
            }
        }

        public bool addTweetNodesAndLikeEdges(int fold) {
            // Tweets that members like: retweet, quote, favorite
            foreach (long memberId in memberIDs.Keys) {
                // Node index of given member
                int idxMember = userIDs[memberId];

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
                    // The number of likes of ego user
                    nLikesOfEgoUser = likes.Count;

                    // The number of tweets the ego user likes should be more than # of folds.
                    if (likes.Count < nFolds) {
                        Console.WriteLine("ERROR: The number of like history is less than nFolds.");
                        Console.WriteLine("\t* # of likes: " + likes.Count);
                        Console.WriteLine("\t* # of folds: " + nFolds);
                        return false;
                    }

                    // Split ego user's like history into two
                    var data = splitLikeHistory(likes, fold);
                    foreach (long like in data.Key) {               // Likes except test tweets
                        addTweetNode(like, NodeType.ITEM);
                        addLink(idxMember, tweetIDs[like], EdgeType.LIKE, 1);
                        addLink(tweetIDs[like], idxMember, EdgeType.LIKE, 1);
                    }

                    // Set test set
                    testSet = data.Value;
                } else {
                    foreach (long like in likes) {
                        addTweetNode(like, NodeType.ITEM);
                        addLink(idxMember, tweetIDs[like], EdgeType.LIKE, 1);
                        addLink(tweetIDs[like], idxMember, EdgeType.LIKE, 1);
                    }
                }
            }

            return true;
        }

        public void addFriendship() {
            addFollowship(false);
        }

        public void addFriendshipAndFollowship() {
            addFollowship(true);
        }

        public void addFollowship(bool inclThirdParty) {
            foreach (long memberId in memberIDs.Keys) {
                // Node index of given member
                int idxMember = userIDs[memberId];

                HashSet<long> followees = dbAdapter.getFollowingUsers(memberId);
                foreach (long followee in followees) {
                    if (memberIDs.ContainsKey(followee)) {
                        // Add links between members; the member nodes are already included in graph
                        addLink(idxMember, userIDs[followee], EdgeType.FRIENDSHIP, 1);
                        addLink(userIDs[followee], idxMember, EdgeType.FRIENDSHIP, 1);
                    } else {
                        if (inclThirdParty == true) {
                            // Add third part user
                            addUserNode(followee, NodeType.ETC);

                            // Add links between member and third party user
                            addLink(idxMember, userIDs[followee], EdgeType.FOLLOW, 1);
                            addLink(userIDs[followee], idxMember, EdgeType.FOLLOW, 1);
                        }
                    }
                }
            }
        }

        public void addThirdPartyFollowship() {
            foreach (long memberId in memberIDs.Keys) {
                // Node index of given member
                int idxMember = userIDs[memberId];
                
                HashSet<long> followees = dbAdapter.getFollowingUsers(memberId);
                foreach (long followee in followees) {
                    if (!memberIDs.ContainsKey(followee)) {
                        // Add third part user
                        addUserNode(followee, NodeType.ETC);

                        // Add links between member and third party user
                        addLink(idxMember, userIDs[followee], EdgeType.FOLLOW, 1);
                        addLink(userIDs[followee], idxMember, EdgeType.FOLLOW, 1);
                    }
                }
            }
        }

        public void addAuthorship() {
            foreach (long memberId in memberIDs.Keys) {
                // Node index of given member
                int idxMember = userIDs[memberId];

                HashSet<long> timeline = dbAdapter.getAuthorship(memberId);
                foreach (long tweet in timeline) {
                    if (!tweetIDs.ContainsKey(tweet))
                        continue;

                    // Add links between ego network member and tweet written by himself/herself
                    addLink(idxMember, tweetIDs[tweet], EdgeType.AUTHORSHIP, 1);
                    addLink(tweetIDs[tweet], idxMember, EdgeType.AUTHORSHIP, 1);
                }
            }
        }

        public void addMentionCount(bool isUnary) {
            foreach (long memberId in memberIDs.Keys) {
                // Node index of given member
                int idxMember = userIDs[memberId];

                Dictionary<long, int> timeline = dbAdapter.getMentionCounts(memberId);

            }
        }

        public void printGraphInfo() {
            //lock (Program.locker) {
            //    Console.WriteLine("\t* Graph information");
            //    Console.WriteLine("\t\t- # of nodes: " + nNodes
            //        + " - User(" + userIDs.Count + "), Tweet(" + tweetIDs.Count + "), ThirdParty(" + thirdPartyIDs.Count + ")");
            //    Console.WriteLine("\t\t- # of links: " + nLinks);
            //}
        }
    }
}
