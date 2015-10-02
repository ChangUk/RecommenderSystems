using System;
using System.Collections.Generic;
using System.IO;

namespace TweetRecommender {
    public class Data {
        public List<long> egoUsers = new List<long>();
        public Dictionary<long, List<long>> friends;
        public Dictionary<long, List<long>> likes;
        public Dictionary<long, Dictionary<long, List<long>>> authorshipOnLikedTweets;
        public Dictionary<long, Dictionary<long, int>> mentionCounts;
        public Dictionary<long, Dictionary<long, int>> likeCounts;
        public Dictionary<long, Dictionary<long, int>> mutuals;
        public Dictionary<long, List<List<long>>> clusters;

        public Data() {
            friends = new Dictionary<long, List<long>>();
            likes = new Dictionary<long, List<long>>();
            authorshipOnLikedTweets = new Dictionary<long, Dictionary<long, List<long>>>();
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
                if (!authorshipOnLikedTweets.ContainsKey(egoUserId))
                    authorshipOnLikedTweets[egoUserId] = new Dictionary<long, List<long>>();

                long memberId = long.Parse(tokens[1]);
                if (!authorshipOnLikedTweets[egoUserId].ContainsKey(memberId))
                    authorshipOnLikedTweets[egoUserId][memberId] = new List<long>();

                for (int i = 2; i < tokens.Length; i++)
                    authorshipOnLikedTweets[egoUserId][memberId].Add(long.Parse(tokens[i]));
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
