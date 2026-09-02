using System.Xml.Linq;
using FiscalDocuments.Api.DTOs;
using FiscalDocuments.Api.Interfaces;
using FiscalDocuments.Api.Models;
using FiscalDocuments.Api.Data;
using System.Security.Cryptography;
using System.Text;

namespace FiscalDocuments.Api.Services;

public class FiscalDocumentService : IFiscalDocumentService
{
    private readonly FiscalDocumentsDbContext _dbContext;

    public FiscalDocumentService(FiscalDocumentsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    private static string GetXmlHash(XDocument xml)
    {
        var normalizedXml = xml.ToString(SaveOptions.DisableFormatting);

        var bytes = Encoding.UTF8.GetBytes(normalizedXml);

        var hash = SHA256.HashData(bytes);

        return Convert.ToHexString(hash);
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
        var xmlHash = GetXmlHash(xml);

        if (_dbContext.FiscalDocuments.Any(x =>
            x.XmlHash == xmlHash &&
            x.Id != id))
        {
            throw new InvalidOperationException(
                "Este XML já foi processado."
            );
        }

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
        fiscalDocument.IssuerCnpj = GetIssuerCnpj(xml, documentType);
        fiscalDocument.RecipientCnpj = GetRecipientCnpj(xml, documentType);
        fiscalDocument.IssueDate = GetIssueDate(xml);
        fiscalDocument.XmlContent = dto.XmlContent;
        fiscalDocument.XmlHash = xmlHash;

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

        var xmlHash = GetXmlHash(xml);

        var fiscalDocument = new FiscalDocument
        {
            Id = Guid.NewGuid(),
            AccessKey = GetAccessKey(xml, documentType),
            DocumentType = documentType,
            IssuerCnpj = GetIssuerCnpj(xml, documentType),
            RecipientCnpj = GetRecipientCnpj(xml, documentType),
            IssueDate = GetIssueDate(xml),
            XmlContent = dto.XmlContent,
            XmlHash = xmlHash,
            CreatedAt = DateTime.UtcNow
        };
        if (_dbContext.FiscalDocuments
    .Any(x => x.XmlHash == fiscalDocument.XmlHash))
        {
            throw new InvalidOperationException(
                "Este XML já foi processado."
            );
        }

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

        var hasNFSe = xml
              .Descendants()
              .Any(x =>
                  x.Name.LocalName == "Nfse" ||
                  x.Name.LocalName == "CompNfse" ||
                  x.Name.LocalName == "InfNfse");

        if (hasNFSe)
        {
            return FiscalDocumentType.NFSe;
        }

        throw new ArgumentException("Tipo de documento fiscal não suportado.");
    }

    private static string GetAccessKey(XDocument xml, FiscalDocumentType documentType)
    {
        var elementName = documentType switch
        {
            FiscalDocumentType.NFe => "infNFe",
            FiscalDocumentType.CTe => "infCte",
            FiscalDocumentType.NFSe => "InfNfse",
            _ => throw new ArgumentException(
                "Tipo de documento fiscal não suportado.")
        };

        var element = xml
               .Descendants()
               .FirstOrDefault(x =>
                   x.Name.LocalName.Equals(
                       elementName,
                       StringComparison.OrdinalIgnoreCase));


        var id = element?.Attribute("Id")?.Value;

        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException(
                "Não foi possível localizar o identificador do documento fiscal."
            );
        }

        return id
            .Replace("NFe", "", StringComparison.OrdinalIgnoreCase)
            .Replace("CTe", "", StringComparison.OrdinalIgnoreCase)
            .Replace("NFSe", "", StringComparison.OrdinalIgnoreCase);
    }

    private static string GetIssuerCnpj(
        XDocument xml,
        FiscalDocumentType documentType)
    {
        var elementName = documentType == FiscalDocumentType.NFSe
        ? "PrestadorServico"
        : "emit";

        var issuer = xml
            .Descendants()
            .FirstOrDefault(x =>
                x.Name.LocalName.Equals(
                    elementName,
                    StringComparison.OrdinalIgnoreCase));

        return issuer?
            .Descendants()
            .FirstOrDefault(x => x.Name.LocalName.Equals(
                "CNPJ",
                StringComparison.OrdinalIgnoreCase))
            ?.Value ?? string.Empty;
    }

    private static string GetRecipientCnpj(
        XDocument xml,
        FiscalDocumentType documentType)
    {
        var elementName = documentType == FiscalDocumentType.NFSe
        ? "TomadorServico"
        : "dest";

        var recipient = xml
            .Descendants()
            .FirstOrDefault(x =>
                x.Name.LocalName.Equals(
                    elementName,
                    StringComparison.OrdinalIgnoreCase));

        return recipient?
            .Descendants()
            .Elements()
            .FirstOrDefault(x =>
                x.Name.LocalName.Equals(
                    "CNPJ",
                    StringComparison.OrdinalIgnoreCase))
            ?.Value ?? string.Empty;
    }

    private static DateTime GetIssueDate(XDocument xml)
    {
        var issueDate = xml
            .Descendants()
            .FirstOrDefault(x =>
                x.Name.LocalName == "dhEmi" ||
                x.Name.LocalName == "dEmi" ||
            x.Name.LocalName == "DataEmissao");

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