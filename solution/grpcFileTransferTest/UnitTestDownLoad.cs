extern alias Client2;

using grpcFileTransfer.Server;
using System;
using System.IO;
using System.Threading;
using Xunit;

namespace grpcFileTransferTest
{
    public class UnitTestDownLoad : TestCommon, IDisposable
    {
        public UnitTestDownLoad()
        {
            TestControllerItem.Init(
            testFolderPrefix:"TestDownload1"
            , portSrvA: 2234
            , portSrvB: 2235
            , getImplFunc: (sett) =>
            {
                var impl = new FileTransferImpl();
                impl.Init(sett);
                return impl;
            });
        }


        [Theory]
        [InlineData(false, false, 0, 0, 1, 10)]
        public async void DownloadDirectory(bool needToCompress, bool needRouteThrough, int filePartSizeMb, int maxSpeedKbps, int threadCount, int testFileSizeMb)
        {
            var client = new Client2::grpcFileTransfer.Client.FileTransferClient();

            var srvAddr1 = TestControllerItem.ServerAddrA;
            var srvAddr2 = TestControllerItem.ServerAddrB;

            var sourceDirRelativePath = "BackupFiles";

            if (needRouteThrough)
            {

            }
            else
            {
                srvAddr2 = null;
            }


            await client.DownLoadFileAsync(
                  serverUri: srvAddr1                            // source server address
                , routeThrough: srvAddr2                         // file transfer proxy server address
                , fileSourceRelativePath: "Backup\\database.bak" // relative source file path
                , dirSourceRelativePath: "Update\\1344"          // relative source directory path
                , destPath: "d:\\tmp\\backups"                   // absolute destination path
                , needToCompress: needToCompress                 // compress before download
                , compressFilePartSizeMb: testFileSizeMb         // splitted volume size, 0 - no split
                , maxSpeedKbps: maxSpeedKbps                     // download speed limit
                , threadCount: threadCount
                , cancellationToken: new CancellationTokenSource().Token
            );


        }

        [Theory]
        [InlineData(false, false, 0, 0, 1, 10)]
        public async void DownloadFile(bool needToCompress, bool needRouteThrough, int filePartSizeMb, int maxSpeedKbps, int threadCount, int testFileSizeMb)
        {
            var client = new Client2::grpcFileTransfer.Client.FileTransferClient();

            // test download file from serverA
            var testFileName = Guid.NewGuid().ToString() + ".dat";

            var clientDestDirPath = Path.Combine(TestControllerItem.ClientPath, "Received");
            var clientDestFilePath = Path.Combine(clientDestDirPath, testFileName);

            var srvAddr1 = TestControllerItem.ServerAddrA;
            var srvAddr2 = TestControllerItem.ServerAddrB;


            if (needRouteThrough)
            {

            }
            else
            {
                // we don't need route through address
                srvAddr2 = null;

                // create test file in the ServerA download dir

                var path = Path.Combine(TestControllerItem.SettServerA.RootDownloadPath, testFileName);

                TestController.CreateTempFile(path, testFileSizeMb);
            }

            await client.DownLoadFileAsync(
                  serverUri: srvAddr1                    // source server address
                , routeThrough: srvAddr2                 // file transfer proxy server address
                , fileSourceRelativePath: testFileName   // relative source file path
                , dirSourceRelativePath: null            // relative source directory path
                , destPath: clientDestDirPath            // absolute destination path
                , needToCompress: needToCompress         // compress before download
                , compressFilePartSizeMb: filePartSizeMb // splitted volume size, 0 - no split
                , maxSpeedKbps: maxSpeedKbps             // download speed limit
                , threadCount: threadCount
                , cancellationToken: new CancellationTokenSource().Token
            );

            Assert.True(File.Exists(clientDestFilePath), "File is not downloaded");
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
        // 

    }
}
