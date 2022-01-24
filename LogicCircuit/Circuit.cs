using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogicCircuit
{
    public abstract class Circuit
    {
        public List<Gate> nodes;
        //public List<Gate, Gate, Wire> edges;
        public List<Gate> inputs;
        public List<Gate> outputs;

        public abstract void CreateCircuit();

    }
}
