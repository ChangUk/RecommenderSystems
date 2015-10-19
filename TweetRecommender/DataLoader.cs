using Recommenders.RWRBased;
using System;
using System.Collections.Generic;
using System.IO;

namespace TweetRecommender {
    public class DataLoader {
        // Ego user's ID
        private long egoUserId;
        public int cntLikesOfEgoUser;

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

            bool exist = false;
            foreach (ForwardLink forwardLink in allLinks[idxSourceNode]) {
                if (forwardLink.targetNode == idxTargetNode && forwardLink.type == type) {
                    exist = true;
                    break;
                }
            }

            if (exist == false) {
                ForwardLink link = new ForwardLink(idxTargetNode, type, weight);
                allLinks[idxSourceNode].Add(link);
                nLinks += 1;
            }
        }

        public bool checkEgoNetworkValidation() {
            int cntLikes = getLikeCountOfEgoUser();
            int cntFriends = getFriendsCountOfEgoUser();
            if (cntLikes < nFolds || cntLikes < 50 || cntFriends < 50) {
                Console.WriteLine("ERROR: The ego network(" + egoUserId + ") is not valid for experiment.");
                Console.WriteLine("\t* # of likes: " + cntLikes);
                Console.WriteLine("\t* # of friends: " + cntFriends);
                Console.WriteLine("\t* # of folds: " + nFolds);
                return false;
            }
            return true;
        }

        public int getLikeCountOfEgoUser() {
            // Tweet IDs a member likes
            HashSet<long> likes = new HashSet<long>();
            HashSet<long> retweets = dbAdapter.getRetweets(egoUserId);
            foreach (long retweet in retweets)
                likes.Add(retweet);
            HashSet<long> quotes = dbAdapter.getQuotedTweets(egoUserId);
            foreach (long quote in quotes)
                likes.Add(quote);
            HashSet<long> favorites = dbAdapter.getFavoriteTweets(egoUserId);
            foreach (long favorite in favorites)
                likes.Add(favorite);
            cntLikesOfEgoUser = likes.Count;
            return likes.Count;
        }

        public int getFriendsCountOfEgoUser() {
            // Get friends of ego network
            HashSet<long> friends = new HashSet<long>();
            HashSet<long> followeesOfEgoUser = dbAdapter.getFollowingUsers(egoUserId);
            foreach (long followee in followeesOfEgoUser) {
                HashSet<long> followeesOfFollowee = dbAdapter.getFollowingUsers(followee);
                if (followeesOfFollowee.Contains(egoUserId))
                    friends.Add(followee);
            }
            return friends.Count;
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

        public void graphConfiguration(Methodology type, int fold) {
            switch (type) {
                case Methodology.BASELINE:                                  // 0
                    graphConfiguration_baseline(fold); break;
                case Methodology.INCL_FRIENDSHIP:                           // 1
                    graphConfiguration_friendship(fold); break;
                case Methodology.INCL_ALLFOLLOWSHIP:                        // 2
                    graphConfiguration_allFollowship(fold); break;
                case Methodology.INCL_AUTHORSHIP:                           // 3
                    graphConfiguration_authorship(fold); break;
                case Methodology.INCL_MENTIONCOUNT:                         // 4
                    graphConfiguration_mentionCount(fold); break;
                case Methodology.ALL:                                       // 5
                    graphConfiguration_all(fold); break;
                case Methodology.EXCL_FOLLOWSHIP:                           // 6
                    graphConfiguration_all_exclFollowship(fold); break;
                case Methodology.EXCL_AUTHORSHIP:                           // 7
                    graphConfiguration_all_exclAuthorship(fold); break;
                case Methodology.EXCL_MENTIONCOUNT:                         // 8
                    graphConfiguration_all_exclMentionCount(fold); break;

            }
            dbAdapter.closeDB();
        }

        /// <summary>
        /// Baseline method
        /// <para>No user friendship</para>
        /// </summary>
        private void graphConfiguration_baseline(int fold) {
            lock (Program.locker)
                Console.WriteLine("Graph(" + egoUserId + " - 0.baseline) Configuration... Fold #" + (fold + 1) + "/" + nFolds);

            // Makeup user and tweet nodes and their relations
            addMemberNodes();
            addTweetNodesAndLikeEdges(fold);

            // Print out the graph information
            printGraphInfo();
        }

        /// <summary>
        /// Propoased method #1
        /// <para>Include friendship relations</para>
        /// </summary>
        private void graphConfiguration_friendship(int fold) {
            lock (Program.locker)
                Console.WriteLine("Graph(" + egoUserId + " - 1.incl_friendship) Configuration... Fold #" + (fold + 1) + "/" + nFolds);

            // Makeup user and tweet nodes and their relations
            addMemberNodes();
            addTweetNodesAndLikeEdges(fold);

            // Add friendship links among network users
            addFriendship();

            // Print out the graph information
            printGraphInfo();
        }

        /// <summary>
        /// Propoased method #2
        /// <para>Include both friendship and followship third party users relations</para>
        /// </summary>
        private void graphConfiguration_allFollowship(int fold) {
            lock (Program.locker)
                Console.WriteLine("Graph(" + egoUserId + " - 2.incl_allFollowship) Configuration... Fold #" + (fold + 1) + "/" + nFolds);

            // Makeup user and tweet nodes and their relations
            addMemberNodes();
            addTweetNodesAndLikeEdges(fold);

            // Add followship links not only among ego network members but also third party users
            addFriendshipAndFollowship();

            // Print out the graph information
            printGraphInfo();
        }

        /// <summary>
        /// Propoased method #3
        /// <para>Include authorship relations</para>
        /// </summary>
        private void graphConfiguration_authorship(int fold) {
            lock (Program.locker)
                Console.WriteLine("Graph(" + egoUserId + " - 3.incl_authorship) Configuration... Fold #" + (fold + 1) + "/" + nFolds);

            // Makeup user and tweet nodes and their relations
            addMemberNodes();
            addTweetNodesAndLikeEdges(fold);

            // Add friendship links among network users
            addFriendship();

            // Add authorship of members
            addAuthorship();

            // Print out the graph information
            printGraphInfo();
        }

        /// <summary>
        /// Propoased method #4
        /// <para>Include mention counts</para>
        /// </summary>
        private void graphConfiguration_mentionCount(int fold) {
            lock (Program.locker)
                Console.WriteLine("Graph(" + egoUserId + " - 4.incl_mentionCount) Configuration... Fold #" + (fold + 1) + "/" + nFolds);

            // Makeup user and tweet nodes and their relations
            addMemberNodes();
            addTweetNodesAndLikeEdges(fold);

            // Add friendship links among network users
            addFriendship();

            // Add mention counts among members
            addMentionCount();

            // Print out the graph information
            printGraphInfo();
        }

        /// <summary>
        /// Propoased method #5
        /// <para>Use all features</para>
        /// </summary>
        private void graphConfiguration_all(int fold) {
            lock (Program.locker)
                Console.WriteLine("Graph(" + egoUserId + " - 5.all features) Configuration... Fold #" + (fold + 1) + "/" + nFolds);

            // Makeup user and tweet nodes and their relations
            addMemberNodes();
            addTweetNodesAndLikeEdges(fold);

            // Add followship links not only among ego network members but also third party users
            addFriendshipAndFollowship();

            // Add authorship of members
            addAuthorship();

            // Add mention counts among members
            addMentionCount();

            // Print out the graph information
            printGraphInfo();
        }

        /// <summary>
        /// Propoased method #6
        /// <para>Use all features without followship on third party</para>
        /// </summary>
        private void graphConfiguration_all_exclFollowship(int fold) {
            lock (Program.locker)
                Console.WriteLine("Graph(" + egoUserId + " - 6.all features excluding followship on third party) Configuration... Fold #" + (fold + 1) + "/" + nFolds);

            // Makeup user and tweet nodes and their relations
            addMemberNodes();
            addTweetNodesAndLikeEdges(fold);

            // Add friendship links among network users
            addFriendship();

            // Add authorship of members
            addAuthorship();

            // Add mention counts among members
            addMentionCount();

            // Print out the graph information
            printGraphInfo();
        }

        /// <summary>
        /// Propoased method #7
        /// <para>Use all features without authorship</para>
        /// </summary>
        private void graphConfiguration_all_exclAuthorship(int fold) {
            lock (Program.locker)
                Console.WriteLine("Graph(" + egoUserId + " - 7.all features excluding authorship) Configuration... Fold #" + (fold + 1) + "/" + nFolds);

            // Makeup user and tweet nodes and their relations
            addMemberNodes();
            addTweetNodesAndLikeEdges(fold);

            // Add followship links not only among ego network members but also third party users
            addFriendshipAndFollowship();

            // Add mention counts among members
            addMentionCount();

            // Print out the graph information
            printGraphInfo();
        }

        /// <summary>
        /// Propoased method #8
        /// <para>Use all features without mention count</para>
        /// </summary>
        private void graphConfiguration_all_exclMentionCount(int fold) {
            lock (Program.locker)
                Console.WriteLine("Graph(" + egoUserId + " - 8.all features excluding mention count) Configuration... Fold #" + (fold + 1) + "/" + nFolds);

            // Makeup user and tweet nodes and their relations
            addMemberNodes();
            addTweetNodesAndLikeEdges(fold);

            // Add followship links not only among ego network members but also third party users
            addFriendshipAndFollowship();

            // Add authorship of members
            addAuthorship();

            // Print out the graph information
            printGraphInfo();
        }

        public void addMemberNodes() {
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

        public void addTweetNodesAndLikeEdges(int fold) {
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
                    // Split ego user's like history into two
                    var data = splitLikeHistory(likes, fold);
                    foreach (long tweet in data.Key) {               // data.Key: training set of like history
                        addTweetNode(tweet, NodeType.ITEM);
                        addLink(idxMember, tweetIDs[tweet], EdgeType.LIKE, 1);
                        addLink(tweetIDs[tweet], idxMember, EdgeType.LIKE, 1);
                    }

                    // Set test set
                    testSet = data.Value;
                } else {
                    foreach (long tweet in likes) {
                        addTweetNode(tweet, NodeType.ITEM);
                        addLink(idxMember, tweetIDs[tweet], EdgeType.LIKE, 1);
                        addLink(tweetIDs[tweet], idxMember, EdgeType.LIKE, 1);
                    }
                }
            }
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

        public void addMentionCount() {
            foreach (long memberId1 in memberIDs.Keys) {
                // Node index of member 1
                int idxMember = userIDs[memberId1];

                // Validation check
                if (!allLinks.ContainsKey(idxMember))
                    continue;

                // Get mention count
                var mentionCounts = new Dictionary<int, int>();
                double sumMentionCount = 0;
                foreach (long memberId2 in memberIDs.Keys) {
                    if (memberId1 == memberId2)
                        continue;

                    int mentionCount = dbAdapter.getMentionCount(memberId1, memberId2);
                    if (mentionCount > 0) {
                        mentionCounts.Add(userIDs[memberId2], mentionCount);
                        sumMentionCount += mentionCount;
                    }
                }

                // Add link with the weight as much as mention frequency
                foreach (int idxTarget in mentionCounts.Keys) {
                    double weight = Math.Log(mentionCounts[idxTarget]) / Math.Log(sumMentionCount);
                    addLink(idxMember, idxTarget, EdgeType.MENTION, weight);
                }
            }
        }

        public void addMentionCount2() {
            foreach (long memberId1 in memberIDs.Keys) {
                // Node index of member 1
                int idxMember = userIDs[memberId1];

                // Validation check
                if (!allLinks.ContainsKey(idxMember))
                    continue;

                // Get mention count
                var mentionCounts = new Dictionary<int, int>();
                double sumLogMentionCount = 0;
                foreach (long memberId2 in memberIDs.Keys) {
                    if (memberId1 == memberId2)
                        continue;

                    int mentionCount = dbAdapter.getMentionCount(memberId1, memberId2);
                    if (mentionCount > 0) {
                        mentionCounts.Add(userIDs[memberId2], mentionCount);
                        sumLogMentionCount += Math.Log(mentionCount);
                    }
                }

                // Get the number of friendship links
                int nFriendhips = 0;
                foreach (ForwardLink friendship in allLinks[idxMember]) {
                    if (friendship.type == EdgeType.FRIENDSHIP)
                        nFriendhips += 1;
                }

                // Add link with the weight as much as mention frequency
                foreach (int idx in mentionCounts.Keys) {
                    double weight = nFriendhips * Math.Log(mentionCounts[idx]) / sumLogMentionCount;
                    addLink(idxMember, idx, EdgeType.MENTION, weight);
                }
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
