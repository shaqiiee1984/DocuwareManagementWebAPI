using DocuWare.Platform.ServerClient;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DocuwareManagementWebAPI.Services
{
    public interface IDocuWareService
    {
        Task<List<Document>> ListAllDocumentsAsync(int count = 1000);
        Task<Document> UploadDocumentAsync(string companyName, string contactName, DateTime birthday, IFormFile file);
        Task<string> DeleteDocumentByIdAsync(string documentId);
    }
}
