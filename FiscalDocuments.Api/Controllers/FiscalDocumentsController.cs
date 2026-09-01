using FiscalDocuments.Api.DTOs;
using FiscalDocuments.Api.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace FiscalDocuments.Api.Controllers;

[ApiController]
[Route("api/fiscal-documents")]
public class FiscalDocumentsController : ControllerBase
{
    private readonly IFiscalDocumentService _fiscalDocumentService;

    public FiscalDocumentsController(
        IFiscalDocumentService fiscalDocumentService)
    {
        _fiscalDocumentService = fiscalDocumentService;
    }

    // O controller recebe a requisição HTTP e delega o processamento do documento fiscal para o serviço.
    [HttpPost]
    public IActionResult Create(CreateFiscalDocumentDto dto)
    {
        var document = _fiscalDocumentService.Create(dto);

        return Created(
            $"/api/fiscal-documents/{document.Id}",
            document
        );
    }
}