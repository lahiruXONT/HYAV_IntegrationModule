using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Integration.Domain.Entities
{
    public class BusinessUnitDBMAP
    {
        public string BusinessUnit { get; set; }=string.Empty;

        public string BusinessUnitName { get; set; }= string.Empty;

        public string DatabaseName { get; set; } = string.Empty;


        public string Division { get; set; } = string.Empty;

    }
}
