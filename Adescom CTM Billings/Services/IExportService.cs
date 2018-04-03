using Adescom_CTM_Billings.Models;
using System.Collections.Generic;

namespace Adescom_CTM_Billings.Services
{
    public interface IExportService
    {
        void FlushOrCreateDirectory(string path);
        void ExportBillingsToPdfs(List<ClientBilling> clientsBillings, string destinationDirectory);
        void ExportBillingsToSummaryPdf(List<ClientBilling> clientsBillings, string destinationDirectory);
        void CreateZipArchive(string sourceDirectory, string destinationFile);
    }
}
