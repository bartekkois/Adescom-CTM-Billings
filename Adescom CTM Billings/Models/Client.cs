using System.Collections.Generic;

namespace Adescom_CTM_Billings.Models
{
    public class Client
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public List<Clid> Clids { get; set; }

        public Client(int id, string name, List<Clid> clids)
        {
            Id = id;
            Name = name;
            Clids = clids;
        }

        public List<string> LiteralLongClids()
        {
            List<string> literalClids = new List<string>();
            foreach(Clid clid in Clids)
                literalClids.Add(clid.Long());

            return literalClids;
        }

        public List<string> LiteralShortClids()
        {
            List<string> literalClids = new List<string>();
            foreach (Clid clid in Clids)
                literalClids.Add(clid.Short());

            return literalClids;
        }
    }
}
