using FiscalDocuments.Api.Models;

namespace FiscalDocuments.Api.Data;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(FiscalDocumentsDbContext context)
    {

        var seedAccessKeys = new[]
        {
            "35260912345678000195550010000000011000000010",
            "35260912345678000195570010000000021000000020",
            "NFS-E-000000003"
        };

        var existingAccessKeys = context.FiscalDocuments
            .Where(d => seedAccessKeys.Contains(d.AccessKey))
            .Select(d => d.AccessKey)
            .ToHashSet();

        var documents = new List<FiscalDocument>
        {
            new FiscalDocument
            {
                Id = Guid.NewGuid(),
                AccessKey = "35260912345678000195550010000000011000000010",
                DocumentType = FiscalDocumentType.NFe,
                IssuerCnpj = "12345678000195",
                RecipientCnpj = "98765432000110",
                IssueDate = DateTime.UtcNow.AddDays(-3),
                XmlContent = """
                <?xml version="1.0" encoding="UTF-8"?>
                <nfeProc>
                    <NFe>
                        <infNFe>
                            <ide>
                                <nNF>000000001</nNF>
                                <serie>1</serie>
                            </ide>
                            <emit>
                                <CNPJ>12345678000195</CNPJ>
                                <xNome>Empresa Exemplo NF-e Ltda</xNome>
                            </emit>
                            <dest>
                                <CNPJ>98765432000110</CNPJ>
                                <xNome>Cliente Exemplo Ltda</xNome>
                            </dest>
                            <total>
                                <ICMSTot>
                                    <vNF>1500.50</vNF>
                                </ICMSTot>
                            </total>
                        </infNFe>
                    </NFe>
                </nfeProc>
                """,
                XmlHash = null,
                CreatedAt = DateTime.UtcNow,
                Active = true
            },

            new FiscalDocument
            {
                Id = Guid.NewGuid(),
                AccessKey = "35260912345678000195570010000000021000000020",
                DocumentType = FiscalDocumentType.CTe,
                IssuerCnpj = "22345678000195",
                RecipientCnpj = "88765432000110",
                IssueDate = DateTime.UtcNow.AddDays(-2),
                XmlContent = """
                <?xml version="1.0" encoding="UTF-8"?>
                <cteProc>
                    <CTe>
                        <infCte>
                            <ide>
                                <nCT>000000002</nCT>
                                <serie>1</serie>
                            </ide>
                            <emit>
                                <CNPJ>22345678000195</CNPJ>
                                <xNome>Transportadora Exemplo Ltda</xNome>
                            </emit>
                            <vPrest>
                                <vTPrest>750.00</vTPrest>
                            </vPrest>
                        </infCte>
                    </CTe>
                </cteProc>
                """,
                XmlHash = null,
                CreatedAt = DateTime.UtcNow,
                Active = true
            },

            new FiscalDocument
            {
                Id = Guid.NewGuid(),
                AccessKey = "NFS-E-000000003",
                DocumentType = FiscalDocumentType.NFSe,
                IssuerCnpj = "32345678000195",
                RecipientCnpj = "78765432000110",
                IssueDate = DateTime.UtcNow.AddDays(-1),
                XmlContent = """
                <?xml version="1.0" encoding="UTF-8"?>
                <CompNfse>
                    <Nfse>
                        <InfNfse>
                            <Numero>000000003</Numero>
                            <PrestadorServico>
                                <Cnpj>32345678000195</Cnpj>
                                <RazaoSocial>Serviços Exemplo Ltda</RazaoSocial>
                            </PrestadorServico>
                            <TomadorServico>
                                <Cnpj>78765432000110</Cnpj>
                                <RazaoSocial>Cliente Serviços Ltda</RazaoSocial>
                            </TomadorServico>
                            <ValorServicos>350.90</ValorServicos>
                        </InfNfse>
                    </Nfse>
                </CompNfse>
                """,
                XmlHash = null,
                CreatedAt = DateTime.UtcNow,
                Active = true
            }
        };

        var newDocuments = documents
            .Where(d => !existingAccessKeys.Contains(d.AccessKey))
            .ToList();

        await context.FiscalDocuments.AddRangeAsync(newDocuments);

        await context.SaveChangesAsync();
    }
}