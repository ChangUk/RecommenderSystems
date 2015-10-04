using Recommenders.RWRBased;
using System;
using System.Collections.Generic;
using System.IO;

namespace TweetRecommender {
    public class NetworkBuilder {
        // Twitter data
        public List<long> egoUsers;
        public Dictionary<long, List<long>> friends;
        public Dictionary<long, List<long>> likes;
        public Dictionary<long, Dictionary<long, List<long>>> authorship;
        public Dictionary<long, Dictionary<long, int>> mentionCounts;
        public Dictionary<long, Dictionary<long, int>> likeCounts;
        public Dictionary<long, Dictionary<long, int>> mutuals;
        public Dictionary<long, List<List<long>>> clusters;

        public NetworkBuilder() {
            egoUsers = new List<long>();
            friends = new Dictionary<long, List<long>>();
            likes = new Dictionary<long, List<long>>();
            authorship = new Dictionary<long, Dictionary<long, List<long>>>();
            mentionCounts = new Dictionary<long, Dictionary<long, int>>();
            likeCounts = new Dictionary<long, Dictionary<long, int>>();
            mutuals = new Dictionary<long, Dictionary<long, int>>();
            clusters = new Dictionary<long, List<List<long>>>();
        }

        public void loadData(string pathData) {
            Console.WriteLine("Loading data...");

            loadEgoUserList(pathData);
            loadFriendsList(pathData);
            loadLikeVectors(pathData);
            loadAuthorshipOnLikedTweets(pathData);
            loadMentionCount(pathData);
            loadLikeCount(pathData);
            loadMutualFriendsCount(pathData);
            loadClusters(pathData);

            Console.WriteLine("\tDone!");

            makeDataPerEgoNetwork(pathData);
        }

        public void makeDataPerEgoNetwork(string pathData) {
            foreach (long egoUser in egoUsers) {
                if (Directory.Exists(pathData + egoUser))
                    continue;

                if (friends.ContainsKey(egoUser)) {
                    string path = pathData + egoUser + "\\users.dat";
                    Directory.CreateDirectory(Path.GetDirectoryName(path));
                    var file = new StreamWriter(path);

                    file.WriteLine(egoUser);
                    foreach (long friend in friends[egoUser])
                        file.WriteLine(friend);
                    file.Close();
                }

                if (true) {
                    string path = pathData + egoUser + "\\likes.dat";
                    Directory.CreateDirectory(Path.GetDirectoryName(path));
                    var file = new StreamWriter(path);

                    if (likes.ContainsKey(egoUser) && likes[egoUser].Count > 0) {
                        file.Write(egoUser);
                        foreach (long tweet in likes[egoUser])
                            file.Write("\t" + tweet);
                        file.WriteLine();
                    }

                    foreach (long friend in friends[egoUser]) {
                        if (likes.ContainsKey(friend) && likes[friend].Count > 0) {
                            file.Write(friend);
                            foreach (long tweet in likes[friend])
                                file.Write("\t" + tweet);
                            file.WriteLine();
                        }
                    }
                    file.Close();
                }

                if (authorship.ContainsKey(egoUser)) {
                    string path = pathData + egoUser + "\\authorship.dat";
                    Directory.CreateDirectory(Path.GetDirectoryName(path));
                    var file = new StreamWriter(path);

                    if (authorship[egoUser].ContainsKey(egoUser) && authorship[egoUser][egoUser].Count > 0) {
                        file.Write(egoUser);
                        foreach (long tweet in authorship[egoUser][egoUser])
                            file.Write("\t" + tweet);
                        file.WriteLine();
                    }

                    foreach (long friend in friends[egoUser]) {
                        if (authorship[egoUser].ContainsKey(friend) && authorship[egoUser][friend].Count > 0) {
                            file.Write(friend);
                            foreach (long tweet in authorship[egoUser][friend])
                                file.Write("\t" + tweet);
                            file.WriteLine();
                        }
                    }
                    file.Close();
                }

                //if (true) {
                //    string path = pathData + egoUser + "\\mentions.dat";
                //    Directory.CreateDirectory(Path.GetDirectoryName(path));
                //    var file = new StreamWriter(path);

                //    if (mentionCounts.ContainsKey(egoUser) && mentionCounts[egoUser].Count > 0) {
                //        file.Write(egoUser);
                //        foreach (KeyValuePair<long, int> entry in mentionCounts[egoUser])
                //            file.WriteLine("\t" + entry.Key + "\t" + entry.Value);
                //    }

                //    foreach (long friend in friends[egoUser]) {
                //        if (likes.ContainsKey(friend) && likes[friend].Count > 0) {
                //            file.Write(friend);
                //            foreach (long tweet in likes[friend])
                //                file.WriteLine("\t" + tweet);
                //        }
                //    }
                //    file.Close();
                //}

                //Dictionary<long, int> _mentionCounts = mentionCounts[egoUser];
                //Dictionary<long, int> _likeCounts = likeCounts[egoUser];
                //Dictionary<long, int> _mutuals = mutuals[egoUser];
                //List<List<long>> _clusters = clusters[egoUser];
            }
        }

        public List<long> getEgoUserList() {
            return egoUsers;
        }

        public KeyValuePair<Dictionary<long, NodeInfo>, Dictionary<long, NodeInfo>> buildBasicNetwork(long egoUserId) {
            Dictionary<long, NodeInfo> userNodes = new Dictionary<long, NodeInfo>();
            Dictionary<long, NodeInfo> itemNodes = new Dictionary<long, NodeInfo>();

            // Make user nodes
            NodeInfo seedNode = new NodeInfo(egoUserId, NodeType.USER);
            userNodes.Add(egoUserId, seedNode);
            foreach (long friendId in friends[egoUserId]) {
                if (!userNodes.ContainsKey(friendId)) {
                    NodeInfo friendNode = new NodeInfo(friendId, NodeType.USER);
                    userNodes.Add(friendId, friendNode);
                }
            }

            // Make tweet nodes
            foreach (long userId in userNodes.Keys) {
                foreach (long tweetId in likes[userId]) {
                    if (!itemNodes.ContainsKey(tweetId)) {
                        NodeInfo tweetNode = new NodeInfo(tweetId, NodeType.ITEM);
                        itemNodes.Add(tweetId, tweetNode);
                    }
                }
            }

            // Make links that a user likes a tweet
            foreach (long userId in userNodes.Keys) {
                foreach (long tweetId in likes[userId]) {
                    NodeInfo userNode = userNodes[userId];
                    NodeInfo itemNode = itemNodes[tweetId];
                    if (userNode.forwardLinks == null)
                        userNode.forwardLinks = new Dictionary<NodeInfo, double>();
                    if (itemNode.forwardLinks == null)
                        itemNode.forwardLinks = new Dictionary<NodeInfo, double>();
                    userNode.forwardLinks.Add(itemNode, 0);
                    itemNode.forwardLinks.Add(userNode, 0);
                }
            }

            return new KeyValuePair<Dictionary<long, NodeInfo>, Dictionary<long, NodeInfo>>(userNodes, itemNodes);
        }

        public void loadEgoUserList(string pathData) {
            StreamReader file = new StreamReader(pathData + "egousers.dat");
            string line;
            while ((line = file.ReadLine()) != null)
                egoUsers.Add(long.Parse(line));
            file.Close();
        }

        public void loadFriendsList(string pathData) {
            StreamReader file = new StreamReader(pathData + "friendlist.dat");
            string line;
            while ((line = file.ReadLine()) != null) {
                string[] tokens = line.Split('\t');
                long userId = long.Parse(tokens[0]);
                List<long> friendList = new List<long>();
                for (int i = 1; i < tokens.Length; i++)
                    friendList.Add(long.Parse(tokens[i]));
                friends[userId] = friendList;
            }
            file.Close();
        }

        public void loadLikeVectors(string pathData) {
            StreamReader file = new StreamReader(pathData + "like_vectors.dat");
            string line;
            while ((line = file.ReadLine()) != null) {
                string[] tokens = line.Split('\t');
                long userId = long.Parse(tokens[0]);
                List<long> tweetList = new List<long>();
                for (int i = 1; i < tokens.Length; i++)
                    tweetList.Add(long.Parse(tokens[i]));
                likes[userId] = tweetList;
            }
            file.Close();
        }

        public void loadAuthorshipOnLikedTweets(string pathData) {
            StreamReader file = new StreamReader(pathData + "authorship_on_likedtweets.dat");
            string line;
            while ((line = file.ReadLine()) != null) {
                string[] tokens = line.Split('\t');
                long userId = long.Parse(tokens[0]);
                List<long> tweetList = new List<long>();
                for (int i = 1; i < tokens.Length; i++)
                    tweetList.Add(long.Parse(tokens[i]));
                likes[userId] = tweetList;
            }
            file.Close();
        }

        public void loadMentionCount(string pathData) {
            StreamReader file = new StreamReader(pathData + "mention_count.dat");
            string line;
            while ((line = file.ReadLine()) != null) {
                string[] tokens = line.Split('\t');
                long egoUserId = long.Parse(tokens[0]);
                if (!authorship.ContainsKey(egoUserId))
                    authorship[egoUserId] = new Dictionary<long, List<long>>();

                long memberId = long.Parse(tokens[1]);
                if (!authorship[egoUserId].ContainsKey(memberId))
                    authorship[egoUserId][memberId] = new List<long>();

                for (int i = 2; i < tokens.Length; i++)
                    authorship[egoUserId][memberId].Add(long.Parse(tokens[i]));
            }
            file.Close();
        }

        public void loadLikeCount(string pathData) {
            StreamReader file = new StreamReader(pathData + "like_count.dat");
            string line;
            while ((line = file.ReadLine()) != null) {
                string[] tokens = line.Split('\t');
                long userId = long.Parse(tokens[0]);
                long targetId = long.Parse(tokens[1]);
                int count = int.Parse(tokens[2]);
                if (!likeCounts.ContainsKey(userId))
                    likeCounts[userId] = new Dictionary<long, int>();
                likeCounts[userId][targetId] = count;
            }
            file.Close();
        }

        public void loadMutualFriendsCount(string pathData) {
            StreamReader file = new StreamReader(pathData + "mutual_friends_count.dat");
            string line;
            while ((line = file.ReadLine()) != null) {
                string[] tokens = line.Split('\t');
                long userId = long.Parse(tokens[0]);
                long targetId = long.Parse(tokens[1]);
                int count = int.Parse(tokens[2]);
                if (!mutuals.ContainsKey(userId))
                    mutuals[userId] = new Dictionary<long, int>();
                mutuals[userId][targetId] = count;
            }
            file.Close();
        }

        public void loadClusters(string pathData) {
            StreamReader file = new StreamReader(pathData + "clusters.dat");
            string line;
            while ((line = file.ReadLine()) != null) {
                string[] tokens = line.Split('\t');
                long egoUserId = long.Parse(tokens[0]);
                if (!clusters.ContainsKey(egoUserId))
                    clusters[egoUserId] = new List<List<long>>();
                List<long> clusterMembers = new List<long>();
                for (int i = 1; i < tokens.Length; i++)
                    clusterMembers.Add(long.Parse(tokens[i]));
                clusters[egoUserId].Add(clusterMembers);
            }
            file.Close();
        }
    }
}
