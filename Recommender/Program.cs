using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recommender {
    public enum NodeType { USER, ITEM }

    class Program {
        static void Main(string[] args) {
            Console.WriteLine("Recommender System starts!");

            RWRBased.Recommender recsys = new RWRBased.Recommender();
            recsys.loadData("C:\\Users\\uklet\\Desktop\\small.dat");
            string targetUserId = "9465097";
            List<KeyValuePair<string, double>> recommendation = recsys.Recommendation(targetUserId);
            foreach (KeyValuePair<string, double> entry in recommendation)
                Console.WriteLine(entry.Value + "\t" + entry.Key);

            Console.WriteLine("Finished!");
        }
    }
}
