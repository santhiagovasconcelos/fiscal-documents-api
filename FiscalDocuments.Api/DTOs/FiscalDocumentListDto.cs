namespace FiscalDocuments.Api.DTOs;

public class FiscalDocumentListDto
{
    public Guid Id { get; set; }
    public string AccessKey { get; set; } = string.Empty;
    public string DocumentType { get; set; } = string.Empty;
    public string IssuerCnpj { get; set; } = string.Empty;
    public string RecipientCnpj { get; set; } = string.Empty;
    public DateTime IssueDate { get; set; }
    public DateTime CreatedAt { get; set; }
}