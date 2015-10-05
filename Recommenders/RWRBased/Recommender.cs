using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recommenders.RWRBased {
    public enum NodeType { UNDEFINED, USER, ITEM, ETC }
    public enum EdgeType { UNDEFINED, LIKE, FRIENDSHIP, FOLLOW, MENTION, ATHORSHIP, PURCHASE, ETC }

    public class Recommender {
        private Graph graph;

        public Recommender(Graph graph) {
            this.graph = graph;
        }

        public List<KeyValuePair<long, double>> Recommendation(int idxTargetUser, float dampingFactor, int nIteration) {
            Model model = new Model(graph, dampingFactor, idxTargetUser);
            model.run(nIteration);

            // Make an exception list of items for target user
            var linksOfTargetUser = new List<int>();
            foreach (ForwardLink link in graph.edges[idxTargetUser])
                linksOfTargetUser.Add(link.targetNode);

            // Make candidate items' list with their rank scores
            var recommendation = new List<KeyValuePair<long, double>>();
            for (int i = 0; i < model.nNodes; i++) {
                if (graph.nodes[i].type == NodeType.ITEM && !linksOfTargetUser.Contains(i))
                    recommendation.Add(new KeyValuePair<long, double>(graph.nodes[i].id, model.rank[i]));
            }

            // Sort the candidate items
            recommendation.Sort((one, another) => { return one.Value.CompareTo(another.Value); });
            recommendation.Reverse();
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
