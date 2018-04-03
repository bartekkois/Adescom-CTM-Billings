using System;
using System.Collections.Generic;

namespace Adescom_CTM_Billings.Models
{
    public class ClientBilling
    {
        public Client Client { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public List<BillingRecord> BillingRecords { get; set; }

        public ClientBilling(Client client, DateTime startDate, DateTime endDate, List<BillingRecord> billingRecords)
        {
            Client = client;
            StartDate = startDate;
            EndDate = endDate;
            BillingRecords = billingRecords;
        }
    }
}
