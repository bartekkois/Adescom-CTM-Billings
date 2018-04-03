using System;

namespace Adescom_CTM_Billings.Models
{
    public class BillingRecord
    {
        public DateTime StartDate { get; set; }
        public string Source { get; set; }
        public string Destination { get; set; }
        public int Duration { get; set; }
        public double Price { get; set; }

        public BillingRecord(DateTime startDate, string source, string destination, int duration, double price)
        {
            StartDate = startDate;
            Source = source;
            Destination = destination;
            Duration = duration;
            Price = price;
        }
    }
}
