using MPCDataClient;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using Xunit;

namespace Tests.DataClientTest
{
    public class UserServiceTest
    {
        UserService userService;
        public UserServiceTest()
        {
            var fileSystemMock = new MockFileSystem();
            this.userService = new UserService(fileSystemMock);
        }

        [Theory]
        [MemberData(nameof(ValidOperations))]
        public void TryParseOperation_ShouldSucceed(string userChoice)
        {
            int operation;
            bool success = userService.TryParseOperation(userChoice, out operation);
            Assert.True(success);
        }

        [Theory]
        [MemberData(nameof(InvalidOperations))]
        public void TryParseOperation_ShouldFail(string userChoice)
        {
            int operation;
            bool success = userService.TryParseOperation(userChoice, out operation);
            Assert.False(success);
        }

        [Theory]
        [MemberData(nameof(ValidFileContent))]
        public void ParseFile_ShouldSuccess(uint[] contentList)
        {
            var filePath = "mock path";
            var fileContent = "title\n" + string.Join('\n', contentList);
            var fileSystemMock = new MockFileSystem();
            fileSystemMock.AddFile(filePath, new MockFileData(fileContent));
            var userService = new UserService(fileSystemMock);
            var outputContent = userService.ParseFile(filePath);
            Assert.Equal(contentList, outputContent);

        }

        [Theory]
        [MemberData(nameof(InvalidFile))]
        public void ParseFile_ShouldThrow(string fileContent, string pathToSend = null)
        {
            var mockPath = "mock path";
            var fileSystemMock = new MockFileSystem();
            fileSystemMock.AddFile(mockPath, new MockFileData(fileContent));
            var userService = new UserService(fileSystemMock);;
            Assert.ThrowsAny<Exception>(() => userService.ParseFile(pathToSend ?? mockPath));

        }


        public static IEnumerable<object[]> ValidOperations()
        {
            yield return new object[]
            {
                "1"
            };

            yield return new object[]
            {
                "3"
            };
        }

        public static IEnumerable<object[]> InvalidOperations()
        {
            yield return new object[]
            {
                0 
            };

            yield return new object[]
            {
                4
            };

            yield return new object[]
            {
                "a"
            };

            yield return new object[]
            {
                null
            };
        }

        public static IEnumerable<object[]> ValidFileContent()
        {            
            yield return new object[]
            {
                new uint[]{1, 2, 3, 4, 5}
            };

            yield return new object[]
            {
                TestUtils.GenerateRandomList(10000)
            };

            yield return new object[]
            {
                new uint[0]
            };
        }

        public static IEnumerable<object[]> InvalidFile()
        {
            yield return new object[]
            {
                "title\n1", "none existing path" //bad file path
            };

            yield return new object[]
            {
                "title\n1\na\n2" //contains none numeric element
            };

            yield return new object[]
            {
                "title\n1\n656000\n2" //contains element value that is bigger than type allows
            };

            yield return new object[]
            {
                "title\n1 2 3" //wrong format
            };
        }
    }
}
