using PDFCorrectionWeb.Models;

namespace PDFCorrectionWeb.Services
{
    public interface IPdfProcessingService : IDisposable
    {
        Task<PdfInfo> GetPdfInfoAsync(string filePath);
        Task AddAnnotationAsync(string filePath, AnnotationRequest annotation);
    }
}