using System.Net;
using System.Net.Mail;
using Microsoft.AspNetCore.Mvc;
using PdfSharp.Pdf;
using PdfSharp.Drawing;
using System.Net.Mime;

namespace PhotoAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PhotoController : ControllerBase
    {
        private const string UploadFolder = "uploads";

        [HttpPost("upload")]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
            var filePath = Path.Combine(UploadFolder, fileName);

            // Garantir que o diretório de upload exista
            Directory.CreateDirectory(UploadFolder);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return Ok(new { FileName = fileName });
        }


        [HttpPost("create-pdf")]
        public IActionResult CreatePdf([FromBody] string[] fileNames)
        {
            var pdfFileName = "output.pdf";
            var outputFilePath = Path.Combine(UploadFolder, pdfFileName);

            using (PdfDocument document = new PdfDocument())
            {
                foreach (var fileName in fileNames)
                {
                    // Ler o conteúdo do arquivo em um MemoryStream
                    byte[] fileBytes = System.IO.File.ReadAllBytes(Path.Combine(UploadFolder, fileName));
                    using (var imageStream = new MemoryStream(fileBytes))
                    {
                        // Criar uma cópia do MemoryStream
                        MemoryStream copiedStream = new MemoryStream();
                        imageStream.CopyTo(copiedStream);
                        copiedStream.Position = 0;

                        PdfPage page = document.AddPage();
                        XGraphics gfx = XGraphics.FromPdfPage(page);
                        XImage image = XImage.FromStream(copiedStream);
                        gfx.DrawImage(image, 0, 0, 595, 842); // You might need to adjust the dimensions according to your requirements
                    }
                }

                document.Save(outputFilePath);
            }

            var pdfFileNameValue = pdfFileName; // Renomeando a variável pdfFileName para evitar conflito com o método File de ControllerBase

            return Ok(new { PdfFileName = pdfFileNameValue });
        }







        [HttpPost("send-email")]
        public IActionResult SendEmail([FromBody] EmailRequest emailRequest)
        {
            var attachmentPath = Path.Combine(UploadFolder, emailRequest.PdfFileName);

            using (var message = new MailMessage())
            {
                message.From = new MailAddress("your_email@example.com");
                message.To.Add(emailRequest.RecipientEmail);
                message.Subject = "PDF Report";
                message.Body = "Please find the attached PDF report.";

                var attachment = new Attachment(attachmentPath, MediaTypeNames.Application.Pdf); // Specifying MIME type for attachment
                message.Attachments.Add(attachment);

                using (var smtpClient = new SmtpClient("smtp.example.com"))
                {
                    smtpClient.UseDefaultCredentials = false;
                    smtpClient.Credentials = new NetworkCredential("your_smtp_username", "your_smtp_password");
                    smtpClient.EnableSsl = true;
                    smtpClient.Port = 587;

                    smtpClient.Send(message);
                }
            }

            return Ok(new { Message = "Email sent successfully." });
        }
    }

    public class EmailRequest
    {
        public string? RecipientEmail { get; set; }
        public string? PdfFileName { get; set; }
    }
}
