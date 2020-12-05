using grpcFileTransfer.Model;
using System;

namespace grpcFileTransfer.Server
{
    public interface IFileTransferSettingsServer : IFileTransferSettingsCommon
    {
        string RootDownloadPath { get; }
        string RootUploadPath { get; }
    }
}
