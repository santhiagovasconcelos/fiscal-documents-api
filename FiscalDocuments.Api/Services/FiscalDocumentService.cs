using System.Xml.Linq;
using FiscalDocuments.Api.DTOs;
using FiscalDocuments.Api.Interfaces;
using FiscalDocuments.Api.Models;

namespace FiscalDocuments.Api.Services;

public class FiscalDocumentService : IFiscalDocumentService
{
    //Recebe do Dto o xml enviado pela API
    // e extrai os dados necessários para montar o documento fiscal.
    public FiscalDocument Create(CreateFiscalDocumentDto dto)
    {
        //validando conteúdo
        if (string.IsNullOrWhiteSpace(dto.XmlContent))
        {
            throw new ArgumentException("O conteúdo XML é obrigatório.");
        }

        XDocument xml;

        try
        {
            // Converte o XML recebido em uma estrutura navegável pelo LINQ to XML.
            xml = XDocument.Parse(dto.XmlContent);
        }
        catch
        {
            throw new ArgumentException("O conteúdo informado não é um XML válido.");
        }

        var documentType = GetDocumentType(xml);

        var fiscalDocument = new FiscalDocument
        {
            Id = Guid.NewGuid(),
            AccessKey = GetAccessKey(xml, documentType),
            DocumentType = documentType,
            IssuerCnpj = GetIssuerCnpj(xml),
            RecipientCnpj = GetRecipientCnpj(xml),
            IssueDate = GetIssueDate(xml),
            XmlContent = dto.XmlContent,
            CreatedAt = DateTime.UtcNow
        };

        return fiscalDocument;
    }

    private static string GetDocumentType(XDocument xml)
    {
        var hasNFe = xml
            .Descendants()
            .Any(x => x.Name.LocalName == "NFe");//localizar as tags sem depender do namespace do XML fiscal.

        if (hasNFe)
        {
            return "NFe";
        }

        var hasCTe = xml
            .Descendants()
            .Any(x => x.Name.LocalName == "CTe");

        if (hasCTe)
        {
            return "CTe";
        }

        throw new ArgumentException("Tipo de documento fiscal não suportado.");
    }

    private static string GetAccessKey(XDocument xml, string documentType)
    {
        var elementName = documentType == "NFe"
            ? "infNFe"
            : "infCte";

        var element = xml
            .Descendants()
            .FirstOrDefault(x => x.Name.LocalName == elementName);

        var id = element?.Attribute("Id")?.Value;

        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException(
                "Não foi possível localizar a chave de acesso."
            );
        }

        return id
            .Replace("NFe", "")
            .Replace("CTe", "");
    }

    private static string GetIssuerCnpj(XDocument xml)
    {
        var issuer = xml
            .Descendants()
            .FirstOrDefault(x => x.Name.LocalName == "emit");

        return issuer?
            .Elements()
            .FirstOrDefault(x => x.Name.LocalName == "CNPJ")
            ?.Value ?? string.Empty;
    }

    private static string GetRecipientCnpj(XDocument xml)
    {
        var recipient = xml
            .Descendants()
            .FirstOrDefault(x => x.Name.LocalName == "dest");

        return recipient?
            .Elements()
            .FirstOrDefault(x => x.Name.LocalName == "CNPJ")
            ?.Value ?? string.Empty;
    }

    private static DateTime GetIssueDate(XDocument xml)
    {
        var issueDate = xml
            .Descendants()
            .FirstOrDefault(x =>
                x.Name.LocalName == "dhEmi" ||
                x.Name.LocalName == "dEmi");

        if (issueDate is null ||
            !DateTime.TryParse(issueDate.Value, out var date))
        {
            throw new ArgumentException(
                "Não foi possível identificar a data de emissão."
            );
        }

        return date;
    }
}