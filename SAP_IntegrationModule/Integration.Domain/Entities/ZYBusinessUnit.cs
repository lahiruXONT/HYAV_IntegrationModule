using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Integration.Domain.Entities;

public class ZYBusinessUnit
{
    public string BusinessUnit { get; set; } = string.Empty;

    public string BusinessUnitName { get; set; } = string.Empty;

    public string Division { get; set; } = string.Empty;

    public string SalesOrganization { get; set; } = string.Empty;
}