using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace WebAppSystems.Services
{
    public class DocumentTextExtractorService
    {
        public async Task<string> ExtractTextAsync(string filePath, string fileExtension)
        {
            return await Task.Run(() =>
            {
                try
                {
                    Console.WriteLine($"[TextExtractor] Extraindo texto de: {filePath}");
                    Console.WriteLine($"[TextExtractor] Extensão: {fileExtension}");
                    
                    string text;
                    switch (fileExtension.ToLower())
                    {
                        case ".pdf":
                            Console.WriteLine($"[TextExtractor] Processando PDF...");
                            text = ExtractTextFromPdf(filePath);
                            break;
                        case ".docx":
                            Console.WriteLine($"[TextExtractor] Processando DOCX...");
                            text = ExtractTextFromDocx(filePath);
                            break;
                        case ".txt":
                            Console.WriteLine($"[TextExtractor] Lendo TXT...");
                            text = File.ReadAllText(filePath);
                            break;
                        default:
                            throw new NotSupportedException($"Tipo de arquivo não suportado: {fileExtension}");
                    }
                    
                    Console.WriteLine($"[TextExtractor] Texto extraído: {text.Length} caracteres");
                    return text;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[TextExtractor] ERRO: {ex.Message}");
                    Console.WriteLine($"[TextExtractor] Stack trace: {ex.StackTrace}");
                    throw new Exception($"Erro ao extrair texto do documento: {ex.Message}", ex);
                }
            });
        }

        private string ExtractTextFromPdf(string filePath)
        {
            var text = new StringBuilder();

            using (var pdfReader = new PdfReader(filePath))
            using (var pdfDocument = new PdfDocument(pdfReader))
            {
                for (int page = 1; page <= pdfDocument.GetNumberOfPages(); page++)
                {
                    var strategy = new SimpleTextExtractionStrategy();
                    var pageText = PdfTextExtractor.GetTextFromPage(pdfDocument.GetPage(page), strategy);
                    text.AppendLine(pageText);
                }
            }

            return text.ToString();
        }

        private string ExtractTextFromDocx(string filePath)
        {
            var text = new StringBuilder();

            using (var wordDocument = WordprocessingDocument.Open(filePath, false))
            {
                var body = wordDocument.MainDocumentPart.Document.Body;
                
                foreach (var paragraph in body.Descendants<Paragraph>())
                {
                    text.AppendLine(paragraph.InnerText);
                }
            }

            return text.ToString();
        }

        public bool IsValidFileType(string fileExtension)
        {
            var validExtensions = new[] { ".pdf", ".docx", ".txt" };
            return Array.Exists(validExtensions, ext => ext.Equals(fileExtension, StringComparison.OrdinalIgnoreCase));
        }

        public long GetMaxFileSizeInBytes()
        {
            return 10 * 1024 * 1024; // 10 MB
        }
    }
}
