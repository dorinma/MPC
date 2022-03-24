using MPCDataClient;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Tests.DataClientTest
{
    public class DataServiceTest
    {
        DataService dataService { get; set; }
        public DataServiceTest()
        {
            this.dataService = new DataService();
        }

        [Theory]
        [MemberData(nameof(Data))]
        public void CanAddTheoryMemberDataProperty(List<UInt16> inputList)
        {
            this.dataService.generateSecretShares(inputList);
            Assert.Equal(inputList, SumList(dataService.serverAList, dataService.serverBList));
        }

        private List<UInt16> SumList(List<UInt16> listA, List<UInt16> listB)
        {
            return (List<UInt16>)listA.Zip(listB, SumUints).ToList();
        }

        private UInt16 SumUints(UInt16 a, UInt16 b)
        {
            return (UInt16)(a + b);
        }

        public static IEnumerable<object[]> Data() {
            Random random = new Random();
            var bigList = new List<UInt16>();
            for (int i = 0; i < 10000; i++)
            {
                bigList.Add((UInt16)random.Next(1000));
            }

            yield return new object[]
            {
                bigList
            };

            yield return new object[]
            {
                new List<UInt16> { 1, 2, 3 }
            };

            yield return new object[]
            {
                new List<UInt16>()
            };
        }
    }
}
