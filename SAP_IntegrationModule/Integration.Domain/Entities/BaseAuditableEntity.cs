using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Integration.Domain.Entities;

public abstract class BaseAuditableEntity
{
    public DateTime UpdatedOn { get; set; }

    [StringLength(40)]
    public string UpdatedBy { get; set; } = string.Empty;
    public DateTime CreatedOn { get; set; }
    [StringLength(40)]
    public string CreatedBy { get; set; } = string.Empty;
}
