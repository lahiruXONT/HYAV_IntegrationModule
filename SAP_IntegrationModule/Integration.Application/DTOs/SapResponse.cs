using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Integration.Application.DTOs;


public class SapODataResponse<T>
{
    public SapODataResults<T> D { get; set; } = new();
}

public class SapODataResults<T>
{
    public List<T> Results { get; set; } = new();
}
