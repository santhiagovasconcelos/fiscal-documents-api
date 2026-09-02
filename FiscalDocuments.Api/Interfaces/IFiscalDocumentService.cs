using FiscalDocuments.Api.DTOs;
using FiscalDocuments.Api.Models;

namespace FiscalDocuments.Api.Interfaces;

public interface IFiscalDocumentService
{
    FiscalDocument Create(CreateFiscalDocumentDto dto);
    List<FiscalDocumentListDto> GetAll(
        int page,
        int pageSize,
        string? documentType,
        string? cnpj
    );
    FiscalDocument? Update(Guid id, UpdateFiscalDocumentDto dto);
    FiscalDocument? GetById(Guid id);
    bool Delete(Guid id);
}