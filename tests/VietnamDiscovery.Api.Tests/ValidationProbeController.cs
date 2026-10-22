using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace VietnamDiscovery.Api.Tests;

[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("_tests/validation")]
public sealed class ValidationProbeController : ControllerBase
{
    [HttpPost]
    public IActionResult Post(ValidationInput input) => Ok(input);
}

public sealed class ValidationInput
{
    [Required]
    public string? Name { get; init; }
}
