using System;
using System.Threading;
using System.Threading.Tasks;

namespace grpcFileTransfer.Client
{
    public class FileTransferClient
    {
        public async Task DownLoadFileAsync(
            string serverUri
            , string routeThrough
            , string fileSourceRelativePath
            , string dirSourceRelativePath
            , string destPath
            , bool needToCompress
            , int compressFilePartSizeMb
            , int maxSpeedKbps
            , int threadCount
            , CancellationToken cancellationToken)
        {
            return;
        }

        public async Task UploadFileAsync(
              string serverUri
            , string routeThrough
            , string fileSourcePath
            , string dirSourcePath
            , string destRelativePath
            , bool needToCompress
            , int compressFilePartSizeMb
            , int maxSpeedKbps
            , int threadCount
            , CancellationToken cancellationToken)
        {
            return;
        }
    }
}
