using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Integration.Domain.Entities;

public class BusinessUnitDBMAP
{
    [Key]
    [StringLength(4)]
    public string BusinessUnit { get; set; } = string.Empty;

    [StringLength(40)]
    public string BusinessUnitName { get; set; } = string.Empty;

    [Required]
    [StringLength(128)]
    public string DatabaseName { get; set; } = string.Empty;

    [Required]
    [StringLength(2)]
    public string Division { get; set; } = string.Empty;
}