using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using grpcFileTransfer.Model;
using grpcFileTransfer.Server.Components;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace grpcFileTransfer.Server
{
    public class FileTransferImpl : FileTransfer.FileTransferBase
    {
        IFileTransferSettingsServer _sett;
        ADownloadSessionController _sessionController = new ADownloadSessionController();

        public void Init(IFileTransferSettingsServer sett)
        {
            _sett = sett;
        }

        public override Task FileDownload(
            FileDownloadRequest request
            , IServerStreamWriter<FileDownloadResponse> responseStream
            , ServerCallContext context)
        {
            try
            {
                switch (request.RequestType)
                {
                    case FileDownloadRequest.Types.FileDownloadRequestTypeEnum.StartSessionMsg:
                        if (_sessionController.SessionKeyIsExists(request.SessionKey))
                        {
                            // the same session key is exists, return error
                            // client should be generate new sessionKey

                            return responseStream.WriteAsync(new FileDownloadResponse
                            {
                                SessionKey = request.SessionKey,
                                MsgType = FileDownloadResponse.Types.FileDownloadResponseType.BadSessionKeyMsg,
                            });
                        }
                        else
                        {
                            // start new download session
                            var session = _sessionController.StartNewSession(
                                request.SessionKey
                                , request.StartSession.RelativeFilePath
                                , responseStream
                                , context
                            );

                            return session.WaitFinish();
                        }

                    case FileDownloadRequest.Types.FileDownloadRequestTypeEnum.ContinueSessionMsg:
                        if (_sessionController.SessionKeyIsExists(request.SessionKey))
                        {
                            // the same session key is exists, it's mean that 
                            // this is adding download call, join it to session
                            var session = _sessionController.JoinToSession(
                                request.SessionKey
                                , responseStream
                                , context
                            );

                            return session.WaitFinish();
                        }
                        else
                        {
                            // it's a mistake, return error
                            return responseStream.WriteAsync(new FileDownloadResponse
                            {
                                SessionKey = request.SessionKey,
                                MsgType = FileDownloadResponse.Types.FileDownloadResponseType.BadSessionKeyMsg,
                            });
                        }

                    default:
                        return responseStream.WriteAsync(new FileDownloadResponse
                        {
                            SessionKey = request.SessionKey,
                            MsgType = FileDownloadResponse.Types.FileDownloadResponseType.ErrorMsg,
                            ErrorData = new FileDownloadResponse.Types.DataErrorItem()
                            {
                                ErrorMessage = $"Unknown download request type - {request.RequestType}"
                            }
                        });
                }
            }
            catch (Exception ex)
            {
                return responseStream.WriteAsync(new FileDownloadResponse
                {
                    SessionKey = request.SessionKey,
                    MsgType = FileDownloadResponse.Types.FileDownloadResponseType.ErrorMsg,
                    ErrorData = new FileDownloadResponse.Types.DataErrorItem()
                    {
                        ErrorMessage = ex.Message
                    }
                });
            }





            // var fileName = Path.Combine(_sett.RootDownloadPath, request.StartSession.RelativeFilePath);
            // 
            // var fileReader = new AFileReader(fileName);
            // 
            // if (!fileReader.CheckFileExists())
            // {
            //     // file not found, say about it to client and exit
            //     responseStream.WriteAsync(new FileDownloadResponse
            //     {
            //         SessionKey = request.SessionKey,
            //         MsgType = FileDownloadResponse.Types.FileDownloadResponseType.FileNotExists
            //     }).GetAwaiter().GetResult();
            // 
            //     return Task.CompletedTask;
            // }
            // 
            // var prepareResult = fileReader.PrepareFile(filePartSizeKb: 512, bufferCount: 10);
            // 
            // responseStream.WriteAsync(new FileDownloadResponse
            // {
            //     SessionKey = request.SessionKey,
            //     MsgType = FileDownloadResponse.Types.FileDownloadResponseType.FileInfoMsg,
            //     FileInfoData = new FileDownloadResponse.Types.FileInfoItem()
            //     {
            //         FileLen = (ulong)fileReader.LoadedFileInfo.Length,
            //         FilePartCount = prepareResult.FilePartCount,
            //         FilePartMaxLen = prepareResult.FilePartSize,
            //         FileCheckSum = ByteString.CopyFrom(prepareResult.FileCheckSum)
            //     }
            // }).GetAwaiter().GetResult();
            // 
            // fileReader.StartBufferFillThread();
            // 
            // return Task.CompletedTask;
            // 
            // //return base.FileDownload(request, responseStream, context);
        }

        /// <summary>
        /// Call when client need to interrupt download
        /// </summary>
        /// <param name="request"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public override Task<FileDownloadFinishResponse> FileDownloadFinish(FileDownloadFinishRequest request, ServerCallContext context)
        {
            _sessionController.FinishSession(request.SessionKey);

            return Task.FromResult(new FileDownloadFinishResponse { Result = true });
        }

        public override Task<FileUploadResponse> FileUpload(IAsyncStreamReader<FileUploadPart> requestStream, ServerCallContext context)
        {
            return base.FileUpload(requestStream, context);
        }
    }
}
