using System.Xml.Linq;
using FiscalDocuments.Api.DTOs;
using FiscalDocuments.Api.Interfaces;
using FiscalDocuments.Api.Models;
using FiscalDocuments.Api.Data;

namespace FiscalDocuments.Api.Services;

public class FiscalDocumentService : IFiscalDocumentService
{
    private readonly FiscalDocumentsDbContext _dbContext;

    public FiscalDocumentService(FiscalDocumentsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    // Retorna os documentos cadastrados, priorizando os mais recentes.
    public List<FiscalDocumentListDto> GetAll(
        int page,
        int pageSize,
        string? documentType,
        string? cnpj
    )
    {
        var query = _dbContext.FiscalDocuments.AsQueryable();
        if (!string.IsNullOrWhiteSpace(documentType))
        {
            if (!Enum.TryParse<FiscalDocumentType>(
                documentType,
                true,
                out var parsedDocumentType))
            {
                throw new ArgumentException(
                    "Tipo de documento fiscal inválido."
                );
            }
            query = query.Where(x =>
                x.DocumentType == parsedDocumentType);
        }

        if (!string.IsNullOrWhiteSpace(cnpj))
        {
            query = query.Where(x =>
                x.IssuerCnpj == cnpj ||
                x.RecipientCnpj == cnpj);
        }


        return query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new FiscalDocumentListDto
            {
                Id = x.Id,
                AccessKey = x.AccessKey,
                DocumentType = x.DocumentType.ToString(),
                IssuerCnpj = x.IssuerCnpj,
                RecipientCnpj = x.RecipientCnpj,
                IssueDate = x.IssueDate,
                CreatedAt = x.CreatedAt
            })
            .ToList();
    }

    public FiscalDocument? Update(
    Guid id,
    UpdateFiscalDocumentDto dto)
    {
        var fiscalDocument = _dbContext.FiscalDocuments
            .FirstOrDefault(x => x.Id == id && x.Active);

        if (fiscalDocument is null)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(dto.XmlContent))
        {
            throw new ArgumentException(
                "O conteúdo XML é obrigatório."
            );
        }

        XDocument xml;

        try
        {
            xml = XDocument.Parse(dto.XmlContent);
        }
        catch
        {
            throw new ArgumentException(
                "O conteúdo informado não é um XML válido."
            );
        }

        var documentType = GetDocumentType(xml);
        var accessKey = GetAccessKey(xml, documentType);

        // Impede que a atualização gere uma chave já utilizada por outro documento.
        if (_dbContext.FiscalDocuments.Any(x =>
            x.AccessKey == accessKey &&
            x.Id != id))
        {
            throw new InvalidOperationException(
                "Já existe um documento fiscal com esta chave de acesso."
            );
        }

        fiscalDocument.AccessKey = accessKey;
        fiscalDocument.DocumentType = documentType;
        fiscalDocument.IssuerCnpj = GetIssuerCnpj(xml);
        fiscalDocument.RecipientCnpj = GetRecipientCnpj(xml);
        fiscalDocument.IssueDate = GetIssueDate(xml);
        fiscalDocument.XmlContent = dto.XmlContent;

        _dbContext.SaveChanges();

        return fiscalDocument;
    }

    public bool Delete(Guid id)
    {
        var fiscalDocument = _dbContext.FiscalDocuments
            .FirstOrDefault(x => x.Id == id && x.Active);

        if (fiscalDocument is null)
        {
            return false;
        }

        fiscalDocument.Active = false;

        _dbContext.SaveChanges();

        return true;
    }

    public FiscalDocument? GetById(Guid id)
    {
        return _dbContext.FiscalDocuments
            .FirstOrDefault(x => x.Id == id && x.Active);
    }

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

        if (_dbContext.FiscalDocuments
            .Any(x => x.AccessKey == fiscalDocument.AccessKey))
        {
            throw new InvalidOperationException(
                "Já existe um documento fiscal com esta chave de acesso."
            );
        }

        // Persiste o documento fiscal após a extração dos dados do XML.
        _dbContext.FiscalDocuments.Add(fiscalDocument);
        _dbContext.SaveChanges();

        return fiscalDocument;
    }

    private static FiscalDocumentType GetDocumentType(XDocument xml)
    {
        var hasNFe = xml
            .Descendants()
            .Any(x => x.Name.LocalName == "NFe");//localizar as tags sem depender do namespace do XML fiscal.

        if (hasNFe)
        {
            return FiscalDocumentType.NFe;
        }

        var hasCTe = xml
            .Descendants()
            .Any(x => x.Name.LocalName == "CTe");

        if (hasCTe)
        {
            return FiscalDocumentType.CTe;
        }

        throw new ArgumentException("Tipo de documento fiscal não suportado.");
    }

    private static string GetAccessKey(XDocument xml, FiscalDocumentType documentType)
    {
        var elementName = documentType == FiscalDocumentType.NFe
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
            !DateTimeOffset.TryParse(issueDate.Value, out var date))
        {
            throw new ArgumentException(
                "Não foi possível identificar a data de emissão."
            );
        }

        return date.UtcDateTime;
    }
}