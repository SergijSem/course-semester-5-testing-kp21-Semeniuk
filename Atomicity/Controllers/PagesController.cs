using Microsoft.AspNetCore.Mvc;
using Atomicity.Models;
using System.IO;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System.Text.RegularExpressions;

namespace Atomicity.Controllers
{
    public class PagesController : Controller
    {
        [HttpPost]
        public async Task<IActionResult> UploadDocument(IFormFile file, bool atomicityCheck, bool ambiguityCheck)
        {
            if (file == null || file.Length == 0)
            {
                return Json(new { success = false, message = "Файл не був вибраний." });
            }

            if (file.ContentType != "application/vnd.openxmlformats-officedocument.wordprocessingml.document")
            {
                return Json(new { success = false, message = "Неправильний тип файлу. Потрібен файл формату .docx" });
            }

            var tempFilePath = Path.Combine(Path.GetTempPath(), file.FileName);

            try
            {
                using (var stream = new FileStream(tempFilePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                var checkedFileName = Path.GetFileNameWithoutExtension(file.FileName) + "_checked.docx";
                var checkedFilePath = Path.Combine(Path.GetTempPath(), checkedFileName);

                AddCommentToDocument(tempFilePath, checkedFilePath, atomicityCheck, ambiguityCheck);

                var fileBytes = await System.IO.File.ReadAllBytesAsync(checkedFilePath);

                System.IO.File.Delete(tempFilePath);
                System.IO.File.Delete(checkedFilePath);

                return File(fileBytes, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", checkedFileName);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Сталася помилка: {ex.Message}");
            }
        }

        private static void AddCommentToDocument(string inputPath, string outputPath, bool atomicityCheck, bool ambiguityCheck)
        {
            using var wordDocument = WordprocessingDocument.Open(inputPath, false);
            using var outputDocument = (WordprocessingDocument)wordDocument.Clone(outputPath, true);

            var mainPart = outputDocument.MainDocumentPart;
            if (mainPart == null) return;

            var commentsPart = mainPart.WordprocessingCommentsPart ?? mainPart.AddNewPart<WordprocessingCommentsPart>();
            commentsPart.Comments ??= new Comments();

            var comments = commentsPart.Comments;
            int commentId = comments.Count() + 1;

            if (atomicityCheck)
            {
                CheckAtomicity(comments, ref commentId, mainPart);
            }

            if (ambiguityCheck)
            {
                CheckAmbiguity(comments, ref commentId, mainPart);
            }

            comments.Save();
            mainPart.Document.Save();
        }


        private static void CheckAtomicity(Comments comments, ref int commentId, MainDocumentPart mainPart)
        {
            string author = "Атомарність";
            var body = mainPart.Document.Body;
            if (body == null) return;
            var paragraphs = body.Elements<Paragraph>().ToList();


            int wordCountLimit = 15;

            foreach (var paragraph in paragraphs)
            {
                var text = paragraph.InnerText;

                if (string.IsNullOrWhiteSpace(text)) continue;

                var sentences = Regex.Split(text, @"(?<=[.!?])\s+");

                foreach (var sentence in sentences)
                {
                    var wordCount = sentence.Split(new[] { ' ', '\t', '\n' }, StringSplitOptions.RemoveEmptyEntries).Length;

                    if (wordCount > wordCountLimit)
                    {
                        AddCommentToText(comments, ref commentId,
                            $"Речення містить більше {wordCountLimit} слів і може бути занадто складним для атомарності.",
                            author, mainPart, sentence);
                    }
                }
            }
        }


        private static void AddCommentToText(Comments comments, ref int commentId, string text, string author, MainDocumentPart mainPart, string term)
        {
            var body = mainPart.Document.Body;
            if (body == null) return;
            var runs = body.Descendants<Run>().ToList();


            foreach (var run in runs)
            {
                var runText = run.InnerText;

                if (runText.Contains(term, StringComparison.OrdinalIgnoreCase))
                {
                    var comment = new Comment()
                    {
                        Id = commentId.ToString(),
                        Author = author,
                        Date = DateTime.Now
                    };

                    comment.AppendChild(new Paragraph(new Run(new Text(text))));
                    comments.AppendChild(comment);

                    run.InsertBeforeSelf(new CommentRangeStart() { Id = commentId.ToString() });
                    run.AppendChild(new CommentRangeEnd() { Id = commentId.ToString() });
                    run.AppendChild(new Run(new CommentReference() { Id = commentId.ToString() }));

                    commentId++;
                }
            }
        }

        private static void CheckAmbiguity(Comments comments, ref int commentId, MainDocumentPart mainPart)
        {
            string author = "Двозначність";
            var body = mainPart.Document.Body;
            if (body == null) return;
            var text = body.InnerText;


            var ambiguousTerms = FindAmbiguousTerms(text);

            foreach (var term in ambiguousTerms)
            {
                AddCommentToText(comments, ref commentId,
                    $"Терміну '{term}' - ймовірно є двозначністю. Переконайтесь, що додали уточнення щодо його значення.",
                    author, mainPart, term);
            }
        }

        private static List<string> FindAmbiguousTerms(string text)
        {
            var ambiguousTerms = new List<string>
            {
                "швидк", "зручн", "легк", "прост", "ефективн", "оптималь", "велик", "мал", "сучасн"
            };

            var foundTerms = new List<string>();

            foreach (var term in ambiguousTerms)
            {
                var pattern = $@"\b{Regex.Escape(term)}\w*\b";
                var matches = Regex.Matches(text, pattern, RegexOptions.IgnoreCase);

                foreach (Match match in matches)
                {
                    foundTerms.Add(match.Value);
                }
            }

            return foundTerms.Distinct().ToList();
        }

        public IActionResult Atomicity()
        {
            return View();
        }

        public IActionResult Explanation()
        {
            return View();
        }
    }
}