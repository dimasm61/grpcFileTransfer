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
        public static string RootCdPath = @"c:\ProgramData\FileTransferTest";

        public TestServer ServerA;
        public TestServer ServerB;

        public string ServerAddrA;
        public string ServerAddrB;

        public IFileTransferSettingsServer SettServerA => ServerA.SettServer;
        public IFileTransferSettingsServer SettServerB => ServerA.SettServer;
        public IFileTransferSettingsClient SettClient;

        public void Init(
              string testFolderPrefix
            , string addrTemplate
            , int portSrvA
            , int portSrvB
            , Func<IFileTransferSettingsServer, FileTransfer.FileTransferBase> getImplFunc
            )
        {
            ServerA.Init(RootCdPath
                , testFolderPrefix
                , "01"
                , getImplFunc
                , addrTemplate
                , portSrvA);

            ServerB.Init(RootCdPath
                , testFolderPrefix
                , "02"
                , getImplFunc
                , addrTemplate
                , portSrvB);

            ServerAddrA = $"localhost:{portSrvA}";
            ServerAddrB = $"localhost:{portSrvB}";

            var settClient = new Mock<IFileTransferSettingsClient>();
            settClient.Setup(state => state.ProgramDataTmpCompresPath).Returns($@"{RootCdPath}\{testFolderPrefix}Client\CompressTmp");
            settClient.Setup(state => state.ProgramDataTmpTransferPath).Returns($@"{RootCdPath}\{testFolderPrefix}Client\TransitTmp");
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
