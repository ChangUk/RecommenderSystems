using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RWRBased {
    class Program {
        static void Main(string[] args) {
            Console.WriteLine("The PageRank algorithm starts!");

            Recommender recsys = new Recommender();
            recsys.loadData("C:\\Users\\changuk\\Desktop\\train.dat");
            string targetUserId = "9465097";
            List<KeyValuePair<string, double>> recommendation = recsys.Recommendation(targetUserId);
            foreach (KeyValuePair<string, double> entry in recommendation)
                Console.WriteLine(entry.Value + "\t" + entry.Key);

            Console.WriteLine("Finished!");
        }
    }
}
