using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RWRBased {
    class Program {
        static void Main(string[] args) {
            Console.WriteLine("The PageRank algorithm starts!");

            List<Node> nodes = new List<Node>();
            double threshold = 0.0001d;
            // TODO: add node into list & set threshold for convergence

            PageRank pagerank = new PageRank(nodes, 0.15f);
            pagerank.run(threshold);

            Console.WriteLine("Finished!");
        }
    }
}
