namespace PDFCorrectionWeb.Models
{
    public class PdfCorrectionViewModel
    {
        public string? UploadedFileName { get; set; }
        public string? OriginalFileName { get; set; }
        public int PageCount { get; set; }
        public bool HasPdf => !string.IsNullOrEmpty(UploadedFileName);
    }

    public class PdfInfo
    {
        public int PageCount { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
    }

    public class AnnotationRequest
    {
        public string FileName { get; set; } = string.Empty;
        public int PageNumber { get; set; } = 1;
        public float X { get; set; }
        public float Y { get; set; }
        public string Type { get; set; } = "custom"; // correct, wrong, custom
        public string Shape { get; set; } = "square"; // square, circle
        public string Text { get; set; } = string.Empty;
    }

    public class ErrorViewModel
    {
        public string? RequestId { get; set; }
        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}