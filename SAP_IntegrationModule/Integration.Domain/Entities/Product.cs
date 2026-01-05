using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Integration.Domain.Entities;

public class Product : GlobalProduct
{
    public string BusinessUnit { get; set; } = string.Empty;
    //public string SalesOrganization { get; set; } = string.Empty;
    //public string DistributionChannel { get; set; } = string.Empty;
    //public string Division { get; set; } = string.Empty;


    //need to get specific info
}
