using DocuWare.Platform.ServerClient;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using DocuwareManagementWebAPI.Models;

namespace DocuwareManagementWebAPI.Services
{
    public class DocuWareService : IDocuWareService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<DocuWareService> _logger;
        //private readonly IServiceConnectionFactory _serviceConnectionFactory;


        /// <summary>
        /// Initializes a new instance of the <see cref="DocuWareService"/> class with the specified configuration and logger.
        /// </summary>
        /// <param name="config">The configuration containing the DocuWare connection settings, such as PlatformUri, UserName, and Password.</param>
        /// <param name="logger">The logger used to log information and errors.</param>
        /// 
        public DocuWareService(IConfiguration config, ILogger<DocuWareService> logger)
        {
            _config = config;
            _logger = logger;
        
        }

        //public DocuWareService(IConfiguration config, ILogger<DocuWareService> logger, IServiceConnectionFactory serviceConnectionFactory)
        //{
        //    _config = config;
        //    _logger = logger;
        //    _serviceConnectionFactory = serviceConnectionFactory;
        //}


        /// <summary>
        /// Establishes a service connection to the DocuWare platform using the configured URI, username, and password.
        /// </summary>
        /// <returns>A <see cref="ServiceConnection"/> object representing the connection to the DocuWare platform.</returns>
        /// <exception cref="UriFormatException">Thrown if the PlatformUri in the configuration is invalid.</exception>
        public ServiceConnection GetServiceConnection()
        {
            _logger.LogInformation("Establishing connection to the DocuWare platform.");

            var platformUri = _config["DocuWare:PlatformUri"];
            var userName = _config["DocuWare:UserName"];
            var password = _config["DocuWare:Password"];

            var uri = new Uri(platformUri);

            _logger.LogInformation("Successfully connected to the DocuWare platform at {PlatformUri}.", platformUri);

            return ServiceConnection.Create(uri, userName, password);

            //return _serviceConnectionFactory.Create(uri, userName, password);

        }

        /// <summary>
        /// Retrieves a list of documents from a DocuWare file cabinet asynchronously.
        /// </summary>
        /// <param name="count">The maximum number of documents to retrieve in a single batch. Defaults to 1000.</param>
        /// <returns>A task that represents the asynchronous operation, which contains a list of documents from the file cabinet.</returns>
        public async Task<List<Document>> ListAllDocumentsAsync(int count = 1000)
        {
            _logger.LogInformation("Attempting to list documents from the file cabinet.");

            var serviceConnection = GetServiceConnection();
            var fileCabinetId = _config["DocuWare:FileCabinetId"];

            try
            {
                DocumentsQueryResult queryResult = await serviceConnection.GetFromDocumentsForDocumentsQueryResultAsync(fileCabinetId, count: count);
                List<Document> documents = new List<Document>();
                await GetAllDocumentsAsync(queryResult, documents);

                _logger.LogInformation("{Count} documents retrieved successfully from the file cabinet.", documents.Count);

                return documents;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while listing documents from the file cabinet.");
                throw;
            }
        }

        /// <summary>
        /// Recursively retrieves documents from a paginated result set and adds them to the provided document list.
        /// </summary>
        /// <param name="queryResult">The current batch of documents returned by the DocuWare service.</param>
        /// <param name="documents">The list to store the retrieved documents.</param>
        /// <returns>A task representing the asynchronous operation of fetching all document batches.</returns>
        private async Task GetAllDocumentsAsync(DocumentsQueryResult queryResult, List<Document> documents)
        {
            documents.AddRange(queryResult.Items);

            if (queryResult.NextRelationLink != null)
            {
                var nextQueryResult = await queryResult.GetDocumentsQueryResultFromNextRelationAsync();
                await GetAllDocumentsAsync(nextQueryResult, documents);
            }
        }

        /// <summary>
        /// Asynchronously uploads a document to the specified DocuWare file cabinet with metadata such as company name, contact name, and birthday.
        /// </summary>
        /// <param name="companyName">The company name associated with the document.</param>
        /// <param name="contactName">The contact name associated with the document.</param>
        /// <param name="birthday">The date of birth associated with the contact.</param>
        /// <param name="file">The file being uploaded, represented as an IFormFile.</param>
        /// <returns>A task representing the asynchronous operation, which contains the uploaded document.</returns>
        public async Task<Document> UploadDocumentAsync(string companyName, string contactName, DateTime birthday, IFormFile file)
        {
            _logger.LogInformation("Attempting to upload a document for {CompanyName} with contact {ContactName}.", companyName, contactName);

            var serviceConnection = GetServiceConnection();
            var fileCabinetId = _config["DocuWare:FileCabinetId"];
            var fileCabinet = serviceConnection.GetFileCabinet(fileCabinetId);

            try
            {
                var indexFields = new List<DocumentIndexField>
                {
                    DocumentIndexField.Create("DWEXTENSION", file.ContentType),
                    DocumentIndexField.Create("COMPANY", companyName),
                    DocumentIndexField.Create("CONTACT", contactName),
                    DocumentIndexField.Create("BIRTHDAY", birthday.ToString("yyyy-MM-dd"))
                };

                var inputDocument = new InputDocument
                {
                    Fields = indexFields,
                    Sections = new List<InputSection>()
                };

                var fileExtension = Path.GetExtension(file.FileName);
                var tempFilePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + fileExtension);

                using (var stream = file.OpenReadStream())
                {
                    using (var tempFileStream = new FileStream(tempFilePath, FileMode.Create))
                    {
                        await file.CopyToAsync(tempFileStream);
                    }
                }

                var tempFileInfo = new FileInfo(tempFilePath);

                var fileUploadInfo = new FileUploadInfo(tempFileInfo)
                {
                    ContentType = file.ContentType
                };

                var inputSection = new InputSection(fileUploadInfo);
                inputDocument.Sections.Add(inputSection);

                var uploadedDocument = await fileCabinet.UploadDocumentAsync(inputDocument).ConfigureAwait(false);

                _logger.LogInformation("Document uploaded successfully: {@UploadedDocument}", uploadedDocument);

                if (File.Exists(tempFilePath))
                {
                    File.Delete(tempFilePath);
                }

                return uploadedDocument;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while uploading the document.");
                throw;
            }
        }

        /// <summary>
        /// Asynchronously deletes a document by its unique ID from the DocuWare file cabinet.
        /// </summary>
        /// <param name="documentId">The unique ID of the document to be deleted.</param>
        /// <returns>A task representing the asynchronous operation that returns a success message if the deletion is successful.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the document is not found or an error occurs during deletion.</exception>
        public async Task<string> DeleteDocumentByIdAsync(string documentId)
        {
            _logger.LogInformation("Attempting to delete document with ID {DocumentId}.", documentId);

            var serviceConnection = GetServiceConnection();
            var fileCabinetId = _config["DocuWare:FileCabinetId"];
            var fileCabinet = serviceConnection.GetFileCabinet(fileCabinetId);

            try
            {
                string result = DeleteDocumentById(fileCabinet, documentId);

                _logger.LogInformation("Document with ID {DocumentId} deleted successfully.", documentId);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting document with ID {DocumentId}.", documentId);
                throw new InvalidOperationException($"Error deleting document with ID {documentId}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Helper method to find and delete a document from the file cabinet by its unique ID.
        /// </summary>
        /// <param name="fileCabinet">The DocuWare file cabinet where the document is stored.</param>
        /// <param name="documentId">The unique ID of the document to be deleted.</param>
        /// <returns>A string message indicating the result of the deletion operation.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the document is not found in the file cabinet.</exception>
        public static string DeleteDocumentById(FileCabinet fileCabinet, string documentId)
        {
            var searchDialog = fileCabinet.GetDialogFromCustomSearchRelation();

            var dialogExpression = new DialogExpression
            {
                Start = 0,
                Count = 1,
                Condition = new List<DialogExpressionCondition>
                {
                    new DialogExpressionCondition
                    {
                        Value = new List<string> { documentId },
                        DBName = "DWDOCID"
                    }
                },
                Operation = DialogExpressionOperation.And
            };

            var foundDocument = searchDialog.GetDialogFromSelfRelation()
                .GetDocumentsResult(dialogExpression).Items.FirstOrDefault();

            if (foundDocument == null)
            {
                throw new InvalidOperationException("Document not found");
            }

            return foundDocument.DeleteSelfRelation();
        }
    }
}
