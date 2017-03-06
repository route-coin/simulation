using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DatabaseRepository;

namespace SimulationTests
{
    [TestClass]
    public class DatabaseRepositoryTests
    {
        [TestMethod]
        public void GetNeighborNodesTest()
        {
            var nodes = DatabaseHelper.GetActiveNodes();
            var neighbors = DatabaseHelper.GetNeighborNodes(nodes[1], 50);
            Assert.IsNotNull(neighbors);
        }
    }
}
