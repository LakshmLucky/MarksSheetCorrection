using Microsoft.AspNetCore.Mvc;
using PDFCorrectionWeb.Models;
using PDFCorrectionWeb.Services;
using System.Diagnostics;

namespace PDFCorrectionWeb.Controllers
{
    public class HomeController : Controller
    {
        private readonly IPdfProcessingService _pdfService;
        private readonly IWebHostEnvironment _environment;

        public HomeController(IPdfProcessingService pdfService, IWebHostEnvironment environment)
        {
            _pdfService = pdfService;
            _environment = environment;
        }

        public IActionResult Index()
        {
            return View(new PdfCorrectionViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> UploadPdf(IFormFile pdfFile)
        {
            if (pdfFile == null || pdfFile.Length == 0)
            {
                return Json(new { success = false, message = "Please select a valid PDF file." });
            }

            if (!pdfFile.ContentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase))
            {
                return Json(new { success = false, message = "Only PDF files are supported." });
            }

            try
            {
                var uploadsPath = Path.Combine(_environment.WebRootPath, "uploads");
                Directory.CreateDirectory(uploadsPath);

                var fileName = Guid.NewGuid().ToString() + ".pdf";
                var filePath = Path.Combine(uploadsPath, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await pdfFile.CopyToAsync(stream);
                }

                var pdfInfo = await _pdfService.GetPdfInfoAsync(filePath);

                return Json(new
                {
                    success = true,
                    fileName = fileName,
                    originalName = pdfFile.FileName,
                    pages = pdfInfo.PageCount,
                    message = $"PDF uploaded successfully. Document contains {pdfInfo.PageCount} page(s)."
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error processing PDF: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> AddAnnotation([FromBody] AnnotationRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.FileName))
                {
                    return Json(new { success = false, message = "Invalid file name provided." });
                }

                var uploadsPath = Path.Combine(_environment.WebRootPath, "uploads");
                var originalPath = Path.Combine(uploadsPath, request.FileName);

                if (!System.IO.File.Exists(originalPath))
                {
                    return Json(new { success = false, message = "Original PDF file not found." });
                }

                // Validate annotation request
                if (request.PageNumber <= 0)
                {
                    return Json(new { success = false, message = "Invalid page number." });
                }

                await _pdfService.AddAnnotationAsync(originalPath, request);

                return Json(new
                {
                    success = true,
                    message = $"Annotation '{request.Text}' added successfully to page {request.PageNumber}."
                });
            }
            catch (UnauthorizedAccessException)
            {
                return Json(new { success = false, message = "File access denied. Please try again." });
            }
            catch (IOException ex)
            {
                return Json(new { success = false, message = $"File operation failed: {ex.Message}" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error adding annotation: {ex.Message}" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> DownloadPdf(string fileName)
        {
            try
            {
                var uploadsPath = Path.Combine(_environment.WebRootPath, "uploads");
                var filePath = Path.Combine(uploadsPath, fileName);

                if (!System.IO.File.Exists(filePath))
                {
                    return NotFound("PDF file not found.");
                }

                var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
                var originalName = fileName.Replace(".pdf", "_corrected.pdf");

                return File(fileBytes, "application/pdf", originalName);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error downloading PDF: {ex.Message}");
            }
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}