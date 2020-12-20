using grpcFileTransfer.Model.Classes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using System.Security.Cryptography;

namespace grpcFileTransfer.Server.Components
{
    /// <summary>
    /// A class that returns file info, checksum and can be used for 
    /// compress/decompress multivolume archives
    /// </summary>
    public class AFileInfo : IDisposable
    {
        public SmLocker Locker = new SmLocker("FileLck");

        private string _fileName;

        public FileInfo LoadedFileInfo;

        public FileStream AFileStream;

        public AFileInfo(string fileName)
        {
            _fileName = fileName;

            LoadedFileInfo = new FileInfo(_fileName);
        }

        public bool CheckFileExists()
        {
            return LoadedFileInfo.Exists;
        }

        public void OpenFile()
        {
            AFileStream = new FileStream(_fileName, FileMode.Open, FileAccess.Read);
        }

        public void CloseFile()
        {
            AFileStream?.Close();
        }

        public bool ValidateFile(string filePath, byte[] checksum)
        {
            if (checksum == null)
                throw new Exception("checksum не должна быть NULL");
            var result = true;

            byte[] md5Result = GetCheckSum(filePath);

            for (var i = 0; i < checksum.Length; i++)
                result &= checksum[i] == md5Result[i];

            return result;
        }

        public static byte[] GetCheckSum(string filePath)
        {
            byte[] md5Result = null;

            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(filePath))
                {
                    md5Result = md5.ComputeHash(stream);

                    stream.Close();
                }
            }

            return md5Result;
        }

        public void Dispose()
        {
            try
            {
                AFileStream?.Dispose();
            }
            catch (Exception)
            {

            }
        }
    }
}