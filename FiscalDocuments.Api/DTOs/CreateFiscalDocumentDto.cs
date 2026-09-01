namespace FiscalDocuments.Api.DTOs;

public class CreateFiscalDocumentDto
{
    //iremos receber apenas o xml e o back vai interpretar
    public string XmlContent { get; set; } = string.Empty;
}