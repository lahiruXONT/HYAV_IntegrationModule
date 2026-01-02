using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Integration.Domain.Entities
{
    public class TerritoryPostalCode
    {
        public long RecID { get; set; }
        [StringLength(4)]
        public string TerritoryCode { get; set; } = string.Empty;
        [StringLength(10)]
        public string PostalCode { get; set; } = string.Empty;
    }
}
