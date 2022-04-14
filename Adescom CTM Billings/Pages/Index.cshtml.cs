using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using System.Linq;
using System.ComponentModel.DataAnnotations;
using Adescom_CTM_Billings.Models;
using Adescom_CTM_Billings.Services;

namespace Adescom_CTM_Billings.Pages
{
    public class IndexModel : PageModel
    {
        [Required]
        [BindProperty]
        public DateTime StartDate { get; set; }

        [Required]
        [BindProperty]
        public DateTime EndDate { get; set; }

        public List<Client> Clients { get; set; }

        public string ExceptionMessage { get; set; }

        private IConfiguration _configuration { get; set; }
        private readonly IWebHostEnvironment _hostingEnvironment;
        private readonly IExportService _exportService;

        public IndexModel(IConfiguration configuration, IExportService exportService, IWebHostEnvironment hostingEnvironment)
        {
            _configuration = configuration;
            _exportService = exportService;
            _hostingEnvironment = hostingEnvironment;

            StartDate = new DateTime(DateTime.Today.AddMonths(-1).Year, DateTime.Today.AddMonths(-1).Month, 3, 00, 00, 00);
            EndDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 2, 23, 59, 59);
        }

        public async Task<IActionResult> OnGet()
        {
            if (!ModelState.IsValid)
            {
                ExceptionMessage = "Wystąpił błąd w zapytaniu";
                return Page();
            }

            try
            {
                AdescomCTM atmanCTMWithProxyClasses = new AdescomCTM(_configuration);
                Clients =  (await atmanCTMWithProxyClasses.GetClientsAsyncWithTimeout(false, null))
                    .OrderBy(o => o.Name)
                    .ToList();

                return Page();
            }
            catch (Exception ex)
            {
                ExceptionMessage = ex.Message;
                return Page();
            }
        }

        public async Task<IActionResult> OnPostExportAsync()
        {
            if (!ModelState.IsValid || StartDate > EndDate)
            {
                ExceptionMessage = "Wystąpił błąd w zapytaniu";
                return Page();
            }

            try
            {
                AdescomCTM atmanCTMWithProxyClasses = new AdescomCTM(_configuration);
                string temporaryPDFDirectory = _hostingEnvironment.ContentRootPath + "/Temp/PDF/";
                string temporaryZIPDirectory = _hostingEnvironment.ContentRootPath + "/Temp/ZIP/";
                string temporaryZIPFile = "Billingi - " + DateTime.Now.ToString("yyyyMMddHHmmssfff") + ".zip";

                Clients = await atmanCTMWithProxyClasses.GetClientsAsyncWithTimeout(true, null);
                List<ClientBilling> clientsBillings = await atmanCTMWithProxyClasses.GetBillingForClientsAsyncWithTimeout(Clients, StartDate, EndDate);

                await Task.Run(() =>
                {
                    _exportService.FlushOrCreateDirectory(temporaryPDFDirectory);
                    _exportService.FlushOrCreateDirectory(temporaryZIPDirectory);
                    _exportService.ExportBillingsToPdfs(clientsBillings, temporaryPDFDirectory);
                    _exportService.ExportBillingsToSummaryPdf(clientsBillings, temporaryPDFDirectory);
                    _exportService.CreateZipArchive(temporaryPDFDirectory, temporaryZIPDirectory + temporaryZIPFile);
                });

                return PhysicalFile(temporaryZIPDirectory + temporaryZIPFile, "application/zip", temporaryZIPFile);
            }
            catch (Exception ex)
            {
                ExceptionMessage = ex.Message;
                return Page();
            }
        }
    }
}
