using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace web_backend;

public class ErrorReason
{
    public string? Field { get; set; }
    public string Reason { get; set; }

    public ErrorReason(string Reason, string? Field = null)
    {
        this.Reason = Reason;
        this.Field = Field;
    }

    public static List<ErrorReason> CreateList(params ErrorReason[] reasons)
    {
        return new List<ErrorReason>(reasons);
    }

    public static BadRequestObjectResult BadRequest(params ErrorReason[] reasons)
    {
        return new BadRequestObjectResult(new List<ErrorReason>(reasons));
    }
}