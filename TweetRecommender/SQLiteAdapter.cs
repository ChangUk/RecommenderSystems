using System;
using System.Collections.Generic;
using System.Data.SQLite;

namespace TweetRecommender {
    public class SQLiteAdapter {
        private SQLiteConnection conn = null;

        public SQLiteAdapter(string dbPath) {
            try {
                this.conn = new SQLiteConnection("Data Source=" + dbPath + "; Version=3;");
                this.conn.Open();
            } catch (Exception e) {
                throw e;
            }
        }

        public SQLiteAdapter(string pathDir, long egoUserId, int level) {
            string dbPath = pathDir + egoUserId + "_" + level + ".sqlite";
            try {
                this.conn = new SQLiteConnection("Data Source=" + dbPath + "; Version=3;");
                this.conn.Open();
            } catch (Exception e) {
                throw e;
            }
        }

        public HashSet<long> getFollowingUsers(long userId) {
            HashSet<long> userList = new HashSet<long>();
            using (SQLiteCommand cmd = new SQLiteCommand(conn)) {
                cmd.CommandText = "SELECT target FROM follow WHERE source = " + userId;
                using (SQLiteDataReader reader = cmd.ExecuteReader()) {
                    while (reader.Read()) {
                        long followee = (long)reader.GetValue(0);
                        userList.Add(followee);
                    }
                }
            }
            return userList;
        }

        public HashSet<long> getAuthorship(long userId) {
            HashSet<long> tweetList = new HashSet<long>();
            using (SQLiteCommand cmd = new SQLiteCommand(conn)) {
                cmd.CommandText = "SELECT id FROM tweet WHERE author = " + userId;
                using (SQLiteDataReader reader = cmd.ExecuteReader()) {
                    while (reader.Read()) {
                        long tweet = (long)reader.GetValue(0);
                        tweetList.Add(tweet);
                    }
                }
            }
            return tweetList;
        }

        public HashSet<long> getRetweets(long userId) {
            HashSet<long> tweetList = new HashSet<long>();
            using (SQLiteCommand cmd = new SQLiteCommand(conn)) {
                cmd.CommandText = "SELECT tweet FROM retweet WHERE user = " + userId;
                using (SQLiteDataReader reader = cmd.ExecuteReader()) {
                    while (reader.Read()) {
                        long tweet = (long)reader.GetValue(0);
                        tweetList.Add(tweet);
                    }
                }
            }
            return tweetList;
        }

        public HashSet<long> getQuotedTweets(long userId) {
            HashSet<long> tweetList = new HashSet<long>();
            using (SQLiteCommand cmd = new SQLiteCommand(conn)) {
                cmd.CommandText = "SELECT tweet FROM quote WHERE user = " + userId;
                using (SQLiteDataReader reader = cmd.ExecuteReader()) {
                    while (reader.Read()) {
                        long tweet = (long)reader.GetValue(0);
                        tweetList.Add(tweet);
                    }
                }
            }
            return tweetList;
        }

        public HashSet<long> getFavoriteTweets(long userId) {
            HashSet<long> tweetList = new HashSet<long>();
            using (SQLiteCommand cmd = new SQLiteCommand(conn)) {
                cmd.CommandText = "SELECT tweet FROM favorite WHERE user = " + userId;
                using (SQLiteDataReader reader = cmd.ExecuteReader()) {
                    while (reader.Read()) {
                        long tweet = (long)reader.GetValue(0);
                        tweetList.Add(tweet);
                    }
                }
            }
            return tweetList;
        }

        public Dictionary<long, int> getMentionCounts(long userId) {
            Dictionary<long, int> mentionCounts = new Dictionary<long, int>();
            using (SQLiteCommand cmd = new SQLiteCommand(conn)) {
                cmd.CommandText = "SELECT target FROM mention WHERE source = " + userId;
                using (SQLiteDataReader reader = cmd.ExecuteReader()) {
                    while (reader.Read()) {
                        long target = (long)reader.GetValue(0);
                        if (!mentionCounts.ContainsKey(target))
                            mentionCounts.Add(target, 1);
                        else
                            mentionCounts[target] += 1;
                    }
                }
            }
            return mentionCounts;
        }
    }
}
