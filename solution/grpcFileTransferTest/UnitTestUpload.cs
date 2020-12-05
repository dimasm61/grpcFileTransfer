extern alias Client2;

using grpcFileTransfer.Server;
using System;
using System.Threading;
using Xunit;

namespace grpcFileTransferTest
{
    public class UnitTestUpload : TestCommon, IDisposable
    {
        public UnitTestUpload()
        {
            TestControllerItem.Init(
                testFolderPrefix: "TestUpload1"
                , portSrvA: 2236
                , portSrvB: 2237
                , getImplFunc: (sett) =>
                {
                    var impl = new FileTransferImpl();
                    impl.Init(sett);
                    return impl;
                });
        }


        [Theory]
        [InlineData(false, false, 0, 1, 10)]
        public async void UploadDirectory(bool needToCompress, bool needRouteThrough, int maxSpeedKbps, int threadCount, int testFileSizeMb)
        {
            var client = new Client2::grpcFileTransfer.Client.FileTransferClient();

            var srvAddr1 = TestControllerItem.ServerAddrA;
            var srvAddr2 = TestControllerItem.ServerAddrB;

            var sourceDirRelativePath = "Releases";

            if (needRouteThrough)
            {

            }
            else
            {
                srvAddr2 = null;
            }

            await client.UploadFileAsync(
                  serverUri: srvAddr1
                , routeThrough: srvAddr2
                , fileSourcePath: "d:\\Backup\\database.bak" // absolute source file path
                , dirSourcePath: "c:\\Update\\1344"          // absolute source directory path
                , destRelativePath: "\\backups"              // relative destination path
                , needToCompress: true             // compress before download
                , compressFilePartSizeMb: 2        // splitted volume size, 0 - no split
                , maxSpeedKbps: 300                // download speed limit
                , threadCount: 5
                , cancellationToken: new CancellationTokenSource().Token
            );


        }

        [Theory]
        [InlineData(false, false, 0, 1, 10)]
        public async void UploadFile(bool needToCompress, bool needRouteThrough, int maxSpeedKbps, int threadCount, int testFileSizeMb)
        {
            var client = new Client2::grpcFileTransfer.Client.FileTransferClient();

            var srvAddr1 = TestControllerItem.ServerAddrA;
            var srvAddr2 = TestControllerItem.ServerAddrB;

            var sourceDirRelativePath = "Releases";

            if (needRouteThrough)
            {

            }
            else
            {
                srvAddr2 = null;
            }

            await client.UploadFileAsync(
                  serverUri: srvAddr1
                , routeThrough: srvAddr2
                , fileSourcePath: "d:\\Backup\\database.bak" // absolute source file path
                , dirSourcePath: "c:\\Update\\1344"          // absolute source directory path
                , destRelativePath: "\\backups"              // relative destination path
                , needToCompress: true                       // compress before download
                , compressFilePartSizeMb: testFileSizeMb     // splitted volume size, 0 - no split
                , maxSpeedKbps: maxSpeedKbps                 // download speed limit
                , threadCount: threadCount
                , cancellationToken: new CancellationTokenSource().Token
            );


        }




        // public async void Test1(bool needToCompress, int maxSpeedKbps, int threadCount)
        // {
        //     var client = new Client2::grpcFileTransfer.Client.FileTransferClient();
        // 
        // 
        // 
        //     await client.DownLoadFileAsync(
        //           serverUri: "10.14.141.104:2525"  // source server address
        //         , routeThrough: "127.0.0.1:2525"   // file transfer proxy server address
        //         , fileSourceRelativePath: "Backup\\database.bak" // relative source file path
        //         , dirSourceRelativePath: "Update\\1344"          // relative source directory path
        //         , destPath: "d:\\tmp\\backups"                   // absolute destination path
        //         , needToCompress: true             // compress before download
        //         , compressFilePartSizeMb: 2        // splitted volume size, 0 - no split
        //         , maxSpeedKbps: 300                // download speed limit
        //         , threadCount: 5
        //         , cancellationToken: new CancellationTokenSource().Token
        //     );
        // 
        //     await client.UploadFileAsync(
        //           serverUri: "10.14.141.104:2525"
        //         , routeThrough: "127.0.0.1:2525"
        //         , fileSourcePath: "d:\\Backup\\database.bak" // absolute source file path
        //         , dirSourcePath: "c:\\Update\\1344"          // absolute source directory path
        //         , destRelativePath: "\\backups"              // relative destination path
        //         , needToCompress: true             // compress before download
        //         , compressFilePartSizeMb: 2        // splitted volume size, 0 - no split
        //         , maxSpeedKbps: 300                // download speed limit
        //         , threadCount: 5
        //         , cancellationToken: new CancellationTokenSource().Token
        //     );
        // }


    }
}
