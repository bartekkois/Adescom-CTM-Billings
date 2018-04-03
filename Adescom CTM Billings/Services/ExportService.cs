using Adescom_CTM_Billings.Models;
using DinkToPdf;
using DinkToPdf.Contracts;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace Adescom_CTM_Billings.Services
{
    public class ExportService : IExportService
    {
        private IConverter _converter;

        public ExportService(IConverter converter)
        {
            _converter = converter;
        }

        public void FlushOrCreateDirectory(string path)
        {
            try
            {
                if (Directory.Exists(path))
                    new DirectoryInfo(path).Delete(true);

                Directory.CreateDirectory(path);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public void ExportBillingsToPdfs(List<ClientBilling> clientsBillings, string destinationDirectory)
        {
            try
            {
                foreach (ClientBilling clientBilling in clientsBillings.OrderBy(c => c.Client.Name))
                {
                    _converter.Convert(new HtmlToPdfDocument()
                    {
                        GlobalSettings = {
                            ColorMode = ColorMode.Grayscale,
                            Orientation = Orientation.Portrait,
                            PaperSize = PaperKind.A4Plus,
                            Margins = new MarginSettings() { Top = 1.5, Bottom = 1.5, Left = 1.5, Right = 1.5, Unit = Unit.Centimeters },
                            Out = destinationDirectory + "Billing połączeń - "+ Utils.RemoveInvalidCharactersFromFilename(clientBilling.Client.Name) + ".pdf",
                            },
                        Objects = {
                                new ObjectSettings() {
                                    PagesCount = true,
                                    HtmlContent =  GenerateHtmlBilling(clientBilling).ToString(),
                                    WebSettings = { DefaultEncoding = "utf-8" },
                                    HeaderSettings = { FontSize = 8, Right = "Strona [page] z [toPage]", Line = false, Spacing = 2.812 }
                                }
                            }
                    });
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public void ExportBillingsToSummaryPdf(List<ClientBilling> clientsBillings, string destinationDirectory)
        {
            try
            {
                _converter.Convert(new HtmlToPdfDocument()
                {
                    GlobalSettings = {
                        ColorMode = ColorMode.Grayscale,
                        Orientation = Orientation.Portrait,
                        PaperSize = PaperKind.A4Plus,
                        Margins = new MarginSettings() { Top = 1.5, Bottom = 1.5, Left = 1.5, Right = 1.5, Unit = Unit.Centimeters },
                        Out = destinationDirectory + "Zestawienie billingów abonentów.pdf",
                        },
                    Objects = {
                            new ObjectSettings() {
                                PagesCount = true,
                                HtmlContent =  GenerateHtmlBillingSummary(clientsBillings).ToString(),
                                WebSettings = { DefaultEncoding = "utf-8" },
                                HeaderSettings = { FontSize = 8, Right = "Strona [page] z [toPage]", Line = false, Spacing = 2.812 }
                            }
                        }
                });
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public void CreateZipArchive(string sourceDirectory, string destinationFile)
        {
            try
            {
                ZipFile.CreateFromDirectory(sourceDirectory, destinationFile);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private static StringBuilder GenerateHtmlBilling(ClientBilling clientBilling)
        {
            string _htmlTemplateHeader = @"
                                <!DOCTYPE html>
                                <html>
                                    <head>
                                        <style>
                                            body {
                                              font-family: Arial, Helvetica, sans-serif; 
                                              font-size: 12px;
                                            }
                                            table {
                                                border-collapse: collapse;
                                                width: 100%;
                                            }
                                            th, td {
                                                text-align: center;
                                                padding: 4px;
                                            }
                                            tr:nth-child(even){
                                                background-color: #f2f2f2
                                            }
                                        </style>
                                    </head>
                                    <body>
                                        Wykaz rozmów abonenta <B>[CLIENT]</B> <BR>
                                        dla numeracji <B>[CLIDS]</B><BR>
                                        za okres od <B>[START_DATE]</B> do <B>[END_DATE]</B><BR>
                                        Koszt: netto <B>[PRICE_NETTO]</B> zł, vat <B>[PRICE_VAT]</B> zł, brutto <B>[PRICE_GROSS]</B> zł<BR>
                                        <P></P>
                                        <table>
                                          <tr>
                                            <th>Lp.</th>
                                            <th>Data i godzina</th>
                                            <th>Numer docelowy</th>
                                            <th>Czas trwania (min:s)</th>
                                            <th>Wartość netto (PLN)</th>
                                          </tr>";

            string _htmlTemplateBody = @" <tr>
                                            <td>[NO]</td>
                                            <td>[START_DATE]</td>
                                            <td>[DESTINATION]</td>
                                            <td>[DURATION]</td>
                                            <td>[PRICE_NETTO]</td>
                                          </tr>";

            string _htmlTemplateFooter = @"</table>
                                        </body>
                                    </html>";

            StringBuilder _htmlClientBilling = new StringBuilder(_htmlTemplateHeader);
            StringBuilder _htmlBillingRecords = new StringBuilder();
            double _priceNettoSum = 0;

            foreach (var (index, billingRecord) in clientBilling.BillingRecords.Enumerate())
            {
                StringBuilder _htmlBillingRecord = new StringBuilder(_htmlTemplateBody);
                _htmlBillingRecord.Replace("[NO]", (index + 1).ToString());
                _htmlBillingRecord.Replace("[START_DATE]", billingRecord.StartDate.ToString());
                _htmlBillingRecord.Replace("[DESTINATION]", billingRecord.Destination.ToString());
                _htmlBillingRecord.Replace("[DURATION]", billingRecord.Duration.ToMinutesAndSeconds().ToString());
                _htmlBillingRecord.Replace("[PRICE_NETTO]", Math.Round(billingRecord.Price, 2).ToString("0.00"));
                _htmlBillingRecords.Append(_htmlBillingRecord);

                _priceNettoSum = _priceNettoSum + billingRecord.Price;
            }

            string clids = "";
            foreach (string clid in Utils.RangifyLiteralValues(clientBilling.Client.LiteralShortClids()))
            {
                clids += clid;
                if (!clid.Equals(Utils.RangifyLiteralValues(clientBilling.Client.LiteralShortClids()).Last()))
                    clids += ",";
            }

            _htmlClientBilling.Replace("[CLIENT]", clientBilling.Client.Name);
            _htmlClientBilling.Replace("[CLIDS]", clids);
            _htmlClientBilling.Replace("[START_DATE]", clientBilling.StartDate.ToString());
            _htmlClientBilling.Replace("[END_DATE]", clientBilling.EndDate.ToString());
            _htmlClientBilling.Replace("[PRICE_NETTO]", Math.Round(_priceNettoSum, 2).ToString("0.00"));
            _htmlClientBilling.Replace("[PRICE_VAT]", Math.Round(_priceNettoSum * 0.23, 2).ToString("0.00"));
            _htmlClientBilling.Replace("[PRICE_GROSS]", Math.Round(_priceNettoSum * 1.23, 2).ToString("0.00"));
            _htmlClientBilling.Append(_htmlBillingRecords);
            _htmlClientBilling.Append(_htmlTemplateFooter);

            return _htmlClientBilling;
        }

        private static StringBuilder GenerateHtmlBillingSummary(List<ClientBilling> clientsBillings)
        {
            string _htmlTemplateHeader = @"
                                <!DOCTYPE html>
                                <html>
                                    <head>
                                        <style>
                                            body {
                                              font-family: Arial, Helvetica, sans-serif; 
                                              font-size: 12px;
                                            }
                                            table {
                                                border-collapse: collapse;
                                                width: 100%;
                                            }
                                            th, td {
                                                text-align: center;
                                                padding: 4px;
                                            }
                                            td.name {
                                                text-align: left;
                                            }
                                            tr:nth-child(even){
                                                background-color: #f2f2f2
                                            }
                                        </style>
                                    </head>
                                    <body>
                                        Zestawienie billingów abonentów<BR>
                                        za okres od <B>[START_DATE]</B> do <B>[END_DATE]</B><BR>
                                        <P></P>
                                        <table>
                                          <tr>
                                            <th>Abonent</th>
                                            <th>Numeracja</th>
                                            <th>Wartość netto (PLN)</th>
                                          </tr>";

            string _htmlTemplateBody = @" <tr>
                                            <td class='name'>[NAME]</td>
                                            <td>[CLIDS]</td>
                                            <td>[PRICE_NETTO]</td>
                                          </tr>";

            string _htmlTemplateFooter = @"</table>
                                        </body>
                                    </html>";

            StringBuilder _htmlClientsBillingsSummary = new StringBuilder(_htmlTemplateHeader);
            _htmlClientsBillingsSummary.Replace("[START_DATE]", clientsBillings.First().StartDate.ToString());
            _htmlClientsBillingsSummary.Replace("[END_DATE]", clientsBillings.First().EndDate.ToString());

            StringBuilder _htmlClientsRecords = new StringBuilder();
            foreach (ClientBilling clientBilling in clientsBillings.OrderBy(c => c.Client.Name))
            {
                string _clids = "";
                foreach (string clid in Utils.RangifyLiteralValues(clientBilling.Client.LiteralShortClids()))
                {
                    _clids += clid;
                    if (!clid.Equals(Utils.RangifyLiteralValues(clientBilling.Client.LiteralShortClids()).Last()))
                        _clids += ",";
                }

                double _priceNettoSum = 0;
                foreach (BillingRecord billingRecord in clientBilling.BillingRecords)
                {
                    _priceNettoSum = _priceNettoSum + billingRecord.Price;
                }

                StringBuilder _htmlClientRecord = new StringBuilder(_htmlTemplateBody);
                _htmlClientRecord.Replace("[NAME]", clientBilling.Client.Name.ToString());
                _htmlClientRecord.Replace("[CLIDS]", _clids.ToString());
                _htmlClientRecord.Replace("[PRICE_NETTO]", Math.Round(_priceNettoSum, 2).ToString("0.00"));
                _htmlClientsRecords.Append(_htmlClientRecord);
            }

            _htmlClientsBillingsSummary.Append(_htmlClientsRecords);
            _htmlClientsBillingsSummary.Append(_htmlTemplateFooter);

            return _htmlClientsBillingsSummary;
        }
    }
}
