using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas;
using iText.Kernel.Colors;
using iText.Kernel.Geom;
using iText.Kernel.Font;
using PDFCorrectionWeb.Models;

namespace PDFCorrectionWeb.Services
{
    public class PdfProcessingService : IPdfProcessingService
    {
        private readonly SemaphoreSlim _fileSemaphore = new(1, 1);

        public async Task<PdfInfo> GetPdfInfoAsync(string filePath)
        {
            return await Task.Run(() =>
            {
                using var reader = new PdfReader(filePath); 
                using var pdfDoc = new PdfDocument(reader);

                return new PdfInfo
                {
                    PageCount = pdfDoc.GetNumberOfPages(),
                    Title = pdfDoc.GetDocumentInfo().GetTitle() ?? "Untitled",
                    Author = pdfDoc.GetDocumentInfo().GetAuthor() ?? "Unknown"
                };
            });
        }

        public async Task AddAnnotationAsync(string filePath, AnnotationRequest annotation)
        {
            await _fileSemaphore.WaitAsync();

            try
            {
                await Task.Run(() =>
                {
                    var tempPath = System.IO.Path.GetTempFileName();
                    var finalTempPath = tempPath + ".pdf";

                    try
                    {
                        // Ensure the source file exists and is accessible
                        if (!File.Exists(filePath))
                        {
                            throw new FileNotFoundException($"Source PDF file not found: {filePath}");
                        }

                        // Copy original to temporary location first
                        File.Copy(filePath, finalTempPath, true);

                        // Process the temporary file
                        using (var fileStream = new FileStream(finalTempPath, FileMode.Open, FileAccess.Read))
                        using (var reader = new PdfReader(fileStream))
                        {
                            var outputPath = System.IO.Path.GetTempFileName() + "_output.pdf";

                            using (var writer = new PdfWriter(outputPath))
                            using (var pdfDoc = new PdfDocument(reader, writer))
                            {
                                var pageCount = pdfDoc.GetNumberOfPages();
                                var targetPage = Math.Min(annotation.PageNumber, pageCount);
                                var page = pdfDoc.GetPage(targetPage);
                                var canvas = new PdfCanvas(page);

                                // Set color based on annotation type
                                DeviceRgb color = annotation.Type switch
                                {
                                    "correct" => (DeviceRgb)DeviceRgb.GREEN,
                                    "wrong" => (DeviceRgb)DeviceRgb.RED,
                                    _ => (DeviceRgb)DeviceRgb.BLUE
                                };

                                canvas.SetStrokeColor(color).SetFillColor(color);

                                // Adjust coordinates to PDF coordinate system
                                var pageSize = page.GetPageSize();
                                var adjustedY = pageSize.GetHeight() - annotation.Y;

                                // Draw shape
                                if (annotation.Shape == "square")
                                {
                                    canvas.Rectangle(annotation.X, adjustedY, 20, 20);
                                }
                                else
                                {
                                    canvas.Circle(annotation.X + 10, adjustedY + 10, 10);
                                }
                                canvas.FillStroke();

                                // Add text if provided
                                if (!string.IsNullOrEmpty(annotation.Text))
                                {
                                    canvas.BeginText()
                                          .SetFontAndSize(PdfFontFactory.CreateFont(), 10)
                                          .SetTextMatrix(annotation.X + 25, adjustedY + 5)
                                          .ShowText(annotation.Text)
                                          .EndText();
                                }

                                // Add symbols for correct/wrong
                                if (annotation.Type == "correct" || annotation.Type == "wrong")
                                {
                                    canvas.SetStrokeColor(DeviceRgb.WHITE).SetLineWidth(2);

                                    if (annotation.Type == "correct")
                                    {
                                        // Draw checkmark
                                        canvas.MoveTo(annotation.X + 5, adjustedY + 10)
                                              .LineTo(annotation.X + 8, adjustedY + 7)
                                              .LineTo(annotation.X + 15, adjustedY + 15)
                                              .Stroke();
                                    }
                                    else
                                    {
                                        // Draw X
                                        canvas.MoveTo(annotation.X + 5, adjustedY + 5)
                                              .LineTo(annotation.X + 15, adjustedY + 15)
                                              .MoveTo(annotation.X + 15, adjustedY + 5)
                                              .LineTo(annotation.X + 5, adjustedY + 15)
                                              .Stroke();
                                    }
                                }

                                canvas.Release();
                            }

                            // Ensure all file handles are closed before file operations
                            GC.Collect();
                            GC.WaitForPendingFinalizers();

                            // Wait a brief moment to ensure file handles are released
                            Thread.Sleep(100);

                            // Replace original with annotated version
                            if (File.Exists(filePath))
                            {
                                File.Delete(filePath);
                            }
                            File.Move(outputPath, filePath);
                        }
                    }
                    finally
                    {
                        // Clean up temporary files
                        CleanupTempFile(tempPath);
                        CleanupTempFile(finalTempPath);
                    }
                });
            }
            finally
            {
                _fileSemaphore.Release();
            }
        }

        private static void CleanupTempFile(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }
            catch
            {
                // Ignore cleanup errors to prevent cascading failures
            }
        }

        public void Dispose()
        {
            _fileSemaphore?.Dispose();
        }
    }
}