namespace Adescom_CTM_Billings.Models
{
    public class Clid
    {
        public string CountryCode { get; set; }
        public string AreaCode { get; set; }
        public string ShortClid { get; set; }

        public Clid(string countryCode, string areaCode, string shortClid)
        {
            CountryCode = countryCode;
            AreaCode = areaCode;
            ShortClid = shortClid;
        }

        public string Short()
        {
            return AreaCode + ShortClid;
        }

        public string Long()
        {
            return CountryCode + AreaCode + ShortClid;
        }
    }
}
