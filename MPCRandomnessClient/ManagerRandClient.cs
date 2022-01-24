using System;
using System.Collections.Generic;

namespace MPCRandomnessClient
{
    class ManagerRandClient
    {
        //Timer timer;
        ExternalSystemAdapter externalSystemAdapter;
        List<LogicCircuit.Types.CIRCUIT_TYPE> types;

        static void Main(string[] args)
        {
        }

        public List<LogicCircuit.Circuit> CreateCircuits(List<LogicCircuit.Types.CIRCUIT_TYPE> pTypes) 
        {
            //For each type in pTypes:
            //Call CreateMasksShares
            //Call GenerateKeys
            return null; 
        }

        private void CreateMasksShares(LogicCircuit.Circuit c1, LogicCircuit.Circuit c2)
        {
        }

        private void GenerateKeys(LogicCircuit.Circuit c1, LogicCircuit.Circuit c2)
        {
        }

    }
}
