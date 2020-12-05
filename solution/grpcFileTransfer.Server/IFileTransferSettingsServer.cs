using grpcFileTransfer.Model;
using System;

namespace grpcFileTransfer.Server
{
    public interface IFileTransferSettingsServer : IFileTransferSettingsCommon
    {
        /// <summary>
        /// The directory from which to download files
        /// </summary>
        string RootDownloadPath { get; }

        /// <summary>
        /// The directory where you can upload files
        /// </summary>
        string RootUploadPath { get; }
    }
}
