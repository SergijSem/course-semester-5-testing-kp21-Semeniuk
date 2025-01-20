using System.ComponentModel.DataAnnotations;

namespace Atomicity.Models
{
    public class DocumentModel
    {
        [Required]
        public string FileName { get; init; }
        public long FileSize { get; set; }
        public string FilePath { get; init; }

        public DocumentModel(string fileName, string filePath) =>
            (FileName, FilePath) = (fileName, filePath);
    }
}