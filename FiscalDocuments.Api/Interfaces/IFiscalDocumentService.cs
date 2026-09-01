using FiscalDocuments.Api.DTOs;
using FiscalDocuments.Api.Models;

namespace FiscalDocuments.Api.Interfaces;

public interface IFiscalDocumentService
{
    FiscalDocument Create(CreateFiscalDocumentDto dto);
}