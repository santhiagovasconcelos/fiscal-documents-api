namespace FiscalDocuments.Api.Models;

public class FiscalDocument
{
    public Guid Id { get; set; }

    public string AccessKey { get; set; } = string.Empty;

    public FiscalDocumentType DocumentType { get; set; }

    public string IssuerCnpj { get; set; } = string.Empty;

    public string RecipientCnpj { get; set; } = string.Empty;

    public DateTime IssueDate { get; set; }

    public string XmlContent { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
    public bool Active { get; set; } = true;
}