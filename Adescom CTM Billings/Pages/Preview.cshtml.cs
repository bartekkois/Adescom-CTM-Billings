using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Adescom_CTM_Billings.Pages
{
    public class PreviewModel : PageModel
    {
        public string Message { get; set; }

        public void OnGet()
        {
            Message = "Your application description page.";
        }
    }
}
