namespace DocuwareManagementWebAPI.Models
{
    using DocuWare.Services.Http.Client;
    using System;
    using System.IO;

    /// <summary>
    /// Provides an implementation of the <see cref="IFileUploadInfo"/> interface that represents file upload information.
    /// </summary>
    public class FileUploadInfo : IFileUploadInfo
    {
        private readonly FileInfo _fileInfo;

        /// <summary>
        /// Initializes a new instance of the <see cref="FileUploadInfo"/> class with the specified <see cref="FileInfo"/>.
        /// </summary>
        /// <param name="fileInfo">The <see cref="FileInfo"/> representing the file to be uploaded.</param>
        public FileUploadInfo(FileInfo fileInfo)
        {
            _fileInfo = fileInfo;
        }

        /// <summary>
        /// Gets or sets the content type (MIME type) of the file.
        /// </summary>
        public string ContentType { get; set; }

        /// <summary>
        /// Gets the name of the file.
        /// </summary>
        public string Name => _fileInfo.Name;

        /// <summary>
        /// Gets the size of the file in bytes.
        /// </summary>
        public long Length => _fileInfo.Length;

        /// <summary>
        /// Gets the last modified time of the file in UTC.
        /// </summary>
        public DateTime LastWriteTimeUtc => _fileInfo.LastWriteTimeUtc;

        /// <summary>
        /// Creates a read-only stream for the file.
        /// </summary>
        /// <returns>A <see cref="Stream"/> object for reading the file content.</returns>
        public Stream CreateStream()
        {
            return _fileInfo.OpenRead();
        }
    }

}
