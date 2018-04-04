using Adescom_CTM_Billings.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Adescom_CTM_Billings.Pages
{
    public class PreviewModel : PageModel
    {
        [Required]
        [BindProperty]
        public DateTime StartDate { get; set; }

        [Required]
        [BindProperty]
        public DateTime EndDate { get; set; }

        [Required]
        [BindProperty]
        public int ClientId { get; set; }

        public ClientBilling ClientBilling { get; set; }

        public string ExceptionMessage { get; set; }

        private IConfiguration _configuration { get; set; }

        public PreviewModel(IConfiguration configuration)
        {
            _configuration = configuration;

            StartDate = new DateTime(DateTime.Today.AddMonths(-1).Year, DateTime.Today.AddMonths(-1).Month, 3, 00, 00, 00);
            EndDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 2, 23, 59, 59);
        }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            if (!ModelState.IsValid || StartDate > EndDate)
            {
                ExceptionMessage = "Wystąpił błąd w zapytaniu";
                return Page();
            }

            try
            {
                ClientId = id;
                AdescomCTM atmanCTMWithProxyClasses = new AdescomCTM(_configuration);
                List<Client> client = (await atmanCTMWithProxyClasses.GetClientsAsyncWithTimeout(true, ClientId));
                ClientBilling = (await atmanCTMWithProxyClasses.GetBillingForClientsAsyncWithTimeout(client, StartDate, EndDate)).FirstOrDefault();
                return Page();
            }
            catch (Exception ex)
            {
                ExceptionMessage = ex.Message;
                return Page();
            }
        }


        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid || StartDate > EndDate)
            {
                ExceptionMessage = "Wystąpił błąd w zapytaniu";
                return Page();
            }

            try
            {
                AdescomCTM atmanCTMWithProxyClasses = new AdescomCTM(_configuration);
                List<Client> client = (await atmanCTMWithProxyClasses.GetClientsAsyncWithTimeout(true, ClientId));
                ClientBilling = (await atmanCTMWithProxyClasses.GetBillingForClientsAsyncWithTimeout(client, StartDate, EndDate)).FirstOrDefault();
                return Page();
            }
            catch (Exception ex)
            {
                ExceptionMessage = ex.Message;
                return Page();
            }
        }
    }
}
