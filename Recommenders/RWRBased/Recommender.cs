using System.Collections.Generic;

namespace Recommenders.RWRBased {
    public enum NodeType { UNDEFINED, USER, ITEM, ETC }
    public enum EdgeType { UNDEFINED, LIKE, FRIENDSHIP, FOLLOW, MENTION, AUTHORSHIP, PURCHASE, ETC }

    public class Recommender {
        private Graph graph;

        public Recommender(Graph graph) {
            this.graph = graph;
        }

        public List<KeyValuePair<long, double>> Recommendation(int idxTargetUser, float dampingFactor, int nIteration) {
            // Run Random Walk with Restart
            Model model = new Model(graph, dampingFactor, idxTargetUser);
            model.run(nIteration);

            // Make an exception list of items for target user
            var linksOfTargetUser = new List<int>();
            foreach (ForwardLink link in graph.edges[idxTargetUser]) {
                if (link.type == EdgeType.LIKE)
                    linksOfTargetUser.Add(link.targetNode);
            }

            // Make candidate items' list with their rank scores
            var recommendation = new List<KeyValuePair<long, double>>();
            for (int i = 0; i < model.nNodes; i++) {
                if (graph.nodes[i].type == NodeType.ITEM && !linksOfTargetUser.Contains(i))
                    recommendation.Add(new KeyValuePair<long, double>(graph.nodes[i].id, model.rank[i]));
            }

            // Sort the candidate items (descending order)
            // Order by rank first, then by item id(time order; the latest one the higher order)
            recommendation.Sort((one, another) => {
                int result = one.Value.CompareTo(another.Value) * -1;
                return result != 0 ? result : one.Key.CompareTo(another.Key) * -1;
            });
            return recommendation;
        }

        public List<KeyValuePair<long, double>> Recommendation(int idxTargetUser, float dampingFactor, int nIteration, int topN) {
            var recommendation = Recommendation(idxTargetUser, dampingFactor, nIteration);
            var topNRecommendation = new List<KeyValuePair<long, double>>();
            for (int i = 0; i < recommendation.Count; i++) {
                topNRecommendation.Add(recommendation[i]);
                if (topNRecommendation.Count == topN)
                    break;
            }
            return topNRecommendation;
        }
    }
}
