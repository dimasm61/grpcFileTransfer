expectations
1. work through proxy server
2. work with congested channel
      - sending by part
      - try and try (polly)
      - speed limit (shaping)
3. be fast (with good channel) - multi threading
4. compressing, splitting to multi volume
5. client interface for progress bar
6. interrupting possiblity
7. interface for setting
8. server should share files  from specified directory only

```
            // examles

            await client.DownLoadFileAsync(
                  serverUri: "10.14.141.104:2525"  // source server address
                , routeThrough: "127.0.0.1:2525"   // file transfer proxy server address
                , fileSourceRelativePath: "Backup\\database.bak" // relative source file path
                , dirSourceRelativePath: "Update\\1344"          // relative source directory path
                , destPath: "d:\\tmp\\backups"                   // absolute destination path
                , needToCompress: true             // compress before download
                , compressFilePartSizeMb: 2        // splitted volume size, 0 - no split
                , maxSpeedKbps: 300                // download speed limit
                , threadCount: 5
                , cancellationToken: new CancellationTokenSource().Token
            );

            await client.UploadFileAsync(
                  serverUri: "10.14.141.104:2525"
                , routeThrough: "127.0.0.1:2525"
                , fileSourcePath: "d:\\Backup\\database.bak" // absolute source file path
                , dirSourcePath: "c:\\Update\\1344"          // absolute source directory path
                , destRelativePath: "\\backups"              // relative destination path
                , needToCompress: true             // compress before download
                , compressFilePartSizeMb: 2        // splitted volume size, 0 - no split
                , maxSpeedKbps: 300                // download speed limit
                , threadCount: 5
                , cancellationToken: new CancellationTokenSource().Token
            );
```
