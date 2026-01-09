using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Integration.Domain.Entities;

public class RetailerClassification : BaseAuditableEntity
{
    public string BusinessUnit { get; set; } = string.Empty;

    public string RetailerCode { get; set; } = string.Empty;

    public string MasterGroup { get; set; } = string.Empty;

    public string MasterGroupDescription { get; set; } = string.Empty;

    public string MasterGroupValue { get; set; } = string.Empty;

    public string MasterGroupValueDescription { get; set; } = string.Empty;

    public string GroupType { get; set; } = string.Empty;

    public string Status { get; set; } = "1";
}
