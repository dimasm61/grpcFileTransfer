using Castle.DynamicProxy.Generators;
using Grpc.Core;
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
    public class TestServer
    {
        public IFileTransferSettingsServer SettServer;
        public Server ServerItem;

        public void Init(
              string rootPath
            , string testFolderPrefix
            , string srvNum
            , Func<IFileTransferSettingsServer, FileTransfer.FileTransferBase> getImplFunc
            , string addr
            , int port)
        {
            var settSrv = new Mock<IFileTransferSettingsServer>();
            settSrv.Setup(state => state.RootUploadPath).Returns($@"{rootPath}\{testFolderPrefix}Server{srvNum}\UploadDest");
            settSrv.Setup(state => state.RootDownloadPath).Returns($@"{rootPath}\{testFolderPrefix}Server{srvNum}\DownloadSource");

            settSrv.Setup(state => state.ProgramDataTmpCompresPath).Returns($@"{rootPath}\{testFolderPrefix}Server{srvNum}\CompressTmp");
            settSrv.Setup(state => state.ProgramDataTmpTransferPath).Returns($@"{rootPath}\{testFolderPrefix}Server{srvNum}\TransitTmp");
            settSrv.Setup(state => state.IsLoaded).Returns(true);

            SettServer = settSrv.Object;

            var impl = getImplFunc?.Invoke(SettServer);

            ServerItem = new Server
            {
                Services = { FileTransfer.BindService(impl) },
                Ports = { new ServerPort(addr, port, ServerCredentials.Insecure) }
            };
        }

        public void Start()
        {
            ServerItem.Start();
        }

        public void Shutdown()
        {
            ServerItem.ShutdownAsync().GetAwaiter().GetResult(); 
        }
    }
}
