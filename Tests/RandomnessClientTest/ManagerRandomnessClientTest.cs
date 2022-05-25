/*using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tests.RandomnessClientTest
{
    public class ManagerRandomnessClientTest
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
            this.dataService.GenerateSecretShares(inputList);
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

        public static IEnumerable<object[]> Data()
        {
            yield return new object[]
            {
                TestUtils.GenerateRandomList(10000)
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
*/