namespace FiscalDocuments.Api.Messaging;

public class FiscalDocumentMessage
{
    public Guid DocumentId { get; set; }
    public DateTime CreatedAt { get; set; }
}