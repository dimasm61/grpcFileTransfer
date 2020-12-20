using Grpc.Core;
using grpcFileTransfer.Model;
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
            // create new int session key
            var sessionKey = BitConverter.ToInt32(Guid.NewGuid().ToByteArray());

            var channel = new Channel(serverUri, ChannelCredentials.Insecure);
            var client = new FileTransfer.FileTransferClient(channel);

            var request = new FileDownloadRequest()
            {
                SessionKey = sessionKey,
                RequestType = FileDownloadRequest.Types.FileDownloadRequestTypeEnum.StartSessionMsg,
                StartSession = new FileDownloadRequest.Types.StartSessionItem()
                {
                    RelativeFilePath = fileSourceRelativePath
                },
            };

            using(var call = client.FileDownload(request))
            {
                while(await call.ResponseStream.MoveNext())
                {
                    var packet = call.ResponseStream.Current;
                    Console.WriteLine($"PacketType:{packet.MsgType}");

                    switch (packet.MsgType)
                    {
                        case FileDownloadResponse.Types.FileDownloadResponseType.BadSessionKeyMsg:
                            break;
                        case FileDownloadResponse.Types.FileDownloadResponseType.FileNotExists:
                            break;
                        case FileDownloadResponse.Types.FileDownloadResponseType.FileInfoMsg:
                            break;
                        case FileDownloadResponse.Types.FileDownloadResponseType.PreparingMsg:
                            break;
                        case FileDownloadResponse.Types.FileDownloadResponseType.DataPartMsg:
                            break;
                        case FileDownloadResponse.Types.FileDownloadResponseType.CompletedMsg:
                            break;
                    }

                }
            }

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
