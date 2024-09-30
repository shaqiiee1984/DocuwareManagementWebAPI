using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using DocuwareManagementWebAPI.Services;
using Microsoft.Extensions.Logging;
using DocuwareManagementWebAPI.Models;

namespace DocuwareManagementWebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DocumentsController : ControllerBase
    {
        private readonly IDocuWareService _docuWareService;
        private readonly ILogger<DocumentsController> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="DocumentsController"/> class with the specified <see cref="DocuWareService"/> and <see cref="ILogger"/>.
        /// </summary>
        /// <param name="docuWareService">The service used to interact with the DocuWare platform.</param>
        /// <param name="logger">The logger instance for logging operations.</param>
        public DocumentsController(IDocuWareService docuWareService, ILogger<DocumentsController> logger)
        {
            _docuWareService = docuWareService;
            _logger = logger;
        }

        /// <summary>
        /// Retrieves a list of documents from the DocuWare file cabinet.
        /// </summary>
        /// <returns>An <see cref="IActionResult"/> containing the list of documents or an error message.</returns>
        [HttpGet("list")]
        public async Task<IActionResult> GetDocuments()
        {
            _logger.LogInformation("Attempting to retrieve documents.");
            try
            {
                var documents = await _docuWareService.ListAllDocumentsAsync();

                if (documents == null || documents.Count == 0)
                {
                    _logger.LogWarning("No documents found.");
                    return NotFound(new { message = "No documents found" });
                }

                _logger.LogInformation("Documents retrieved successfully.");
                return Ok(documents);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving documents.");
                return StatusCode(500, new { message = "Error retrieving documents", details = ex.Message });
            }
        }

        /// <summary>
        /// Uploads a new document to the DocuWare file cabinet along with metadata.
        /// </summary>
        /// <param name="companyName">The company name associated with the document.</param>
        /// <param name="contactName">The contact name associated with the document.</param>
        /// <param name="birthday">The birthday of the contact associated with the document.</param>
        /// <param name="file">The file to be uploaded.</param>
        /// <returns>An <see cref="IActionResult"/> containing the result of the upload operation.</returns>
        [HttpPost("upload")]
        public async Task<IActionResult> UploadDocument(
            [FromForm] string companyName,
            [FromForm] string contactName,
            [FromForm] DateTime birthday,
            [FromForm] IFormFile file)
        {
            _logger.LogInformation("Attempting to upload a document for company {companyName}.", companyName);
            if (file == null || file.Length == 0)
            {
                _logger.LogWarning("No file was uploaded or the file is empty.");
                return BadRequest(new { message = "No file was uploaded or the file is empty." });
            }

            try
            {
                var uploadedDocument = await _docuWareService.UploadDocumentAsync(companyName, contactName, birthday, file);
                _logger.LogInformation("Document uploaded successfully with ID {DocumentId}.", uploadedDocument.Id);
                return Ok(new UploadDocumentResponse { Message = "Document uploaded successfully", DocumentId = uploadedDocument.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while uploading the document.");
                return StatusCode(500, new { message = "Error uploading document", details = ex.Message });
            }
        }


        /// <summary>
        /// Deletes a document by its ID from the DocuWare file cabinet.
        /// </summary>
        /// <param name="id">The unique ID of the document to be deleted.</param>
        /// <returns>An <see cref="IActionResult"/> containing the result of the deletion operation.</returns>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDocument(string id)
        {
            _logger.LogInformation("Attempting to delete document with ID {DocumentId}.", id);
            try
            {
                var result = await _docuWareService.DeleteDocumentByIdAsync(id);
                _logger.LogInformation("Document with ID {DocumentId} deleted successfully.", id);
                return Ok(new DeleteDocumentResponse { Message = "Document deleted successfully", Result = result });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Document with ID {DocumentId} not found.", id);
                return NotFound(new { message = ex.Message });
            }

            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while deleting the document with ID {DocumentId}.", id);
                return StatusCode(500, new { message = "Error deleting document", details = ex.Message });
            }
        }

    }
}
