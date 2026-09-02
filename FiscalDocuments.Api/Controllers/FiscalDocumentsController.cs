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

    // Exclui um documento fiscal pelo identificador (apenas marca como ativo=false).
    [HttpDelete("{id:guid}")]
    public IActionResult Delete(Guid id)
    {
        var deleted = _fiscalDocumentService.Delete(id);

        if (!deleted)
        {
            return NotFound(new
            {
                message = "Documento fiscal não encontrado."
            });
        }

        return NoContent();
    }
    // Atualiza um documento fiscal existente a partir de um novo XML.
    [HttpPut("{id:guid}")]
    public IActionResult Update(
        Guid id,
        UpdateFiscalDocumentDto dto)
    {
        try
        {
            var document = _fiscalDocumentService.Update(id, dto);

            if (document is null)
            {
                return NotFound(new
                {
                    message = "Documento fiscal não encontrado."
                });
            }

            return Ok(document);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new
            {
                message = ex.Message
            });
        }
    }

    [HttpGet("{id:guid}")]
    public IActionResult GetById(Guid id)
    {
        var document = _fiscalDocumentService.GetById(id);

        if (document is null)
        {
            return NotFound(new
            {
                message = "Documento fiscal não encontrado."
            });
        }

        return Ok(document);
    }

    // Lista os documentos fiscais com suporte a paginação e filtros.
    [HttpGet]
    public IActionResult GetAll(
        int page = 1,
        //valor padrão para evitar ter que carregar diversos registros
        int pageSize = 10,
        string? documentType = null,
        string? cnpj = null
    )
    {
        var documents = _fiscalDocumentService.GetAll(
            page,
            pageSize,
            documentType,
            cnpj
        );

        return Ok(documents);
    }

    // O controller recebe a requisição HTTP e delega o processamento do documento fiscal para o serviço.
    [HttpPost]
    public IActionResult Create(CreateFiscalDocumentDto dto)
    {

        try
        {
            var document = _fiscalDocumentService.Create(dto);

            return Created(
                $"/api/fiscal-documents/{document.Id}",
                document
            );
        }
        catch (ArgumentException ex)
        {
            return Conflict(new
            {
                message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new
            {
                message = ex.Message
            });
        }


    }
}