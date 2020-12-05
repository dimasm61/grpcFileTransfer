extern alias Client2;

using Client2::grpcFileTransfer.Client;
using grpcFileTransfer.Model;
using grpcFileTransfer.Server;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace grpcFileTransferTest
{
    public class TestController : IDisposable
    {
        public static string RootProgDataPath = @"c:\ProgramData\FileTransferTest";
        public string ClientPath;

        public TestServer ServerA = new TestServer();
        public TestServer ServerB = new TestServer();

        public string ServerAddrA;
        public string ServerAddrB;

        public IFileTransferSettingsServer SettServerA => ServerA.SettServer;
        public IFileTransferSettingsServer SettServerB => ServerA.SettServer;
        public IFileTransferSettingsClient SettClient;

        public void Init(
              string testFolderPrefix
            , int portSrvA
            , int portSrvB
            , Func<IFileTransferSettingsServer, FileTransfer.FileTransferBase> getImplFunc
            )
        {
            ServerA.Init(RootProgDataPath
                , testFolderPrefix
                , "01"
                , getImplFunc
                , "localhost"
                , portSrvA);

            ServerB.Init(RootProgDataPath
                , testFolderPrefix
                , "02"
                , getImplFunc
                , "localhost"
                , portSrvB);

            ServerAddrA = $"localhost:{portSrvA}";
            ServerAddrB = $"localhost:{portSrvB}";

            var settClient = new Mock<IFileTransferSettingsClient>();
            ClientPath = $@"{RootProgDataPath}\{testFolderPrefix}Client";
            settClient.Setup(state => state.ProgramDataTmpCompresPath ).Returns($@"{ClientPath}\CompressTmp");
            settClient.Setup(state => state.ProgramDataTmpTransferPath).Returns($@"{ClientPath}\TransitTmp");
            settClient.Setup(state => state.IsLoaded).Returns(true);

            SettClient = settClient.Object;

            ServerA.Start();
            ServerB.Start();
        }


        public void Dispose()
        {
            ServerA?.Shutdown();
            ServerB?.Shutdown();
        }

        public static void CreateTempFile(string fileFullPath, int fileSizeMb)
        {

        }
    }
}
