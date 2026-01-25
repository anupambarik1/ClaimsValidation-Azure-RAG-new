using ClaimsRagBot.Application.RAG;
using ClaimsRagBot.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace ClaimsRagBot.Api.Controllers;

/// <summary>
/// Claims validation API endpoints
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ClaimsController : ControllerBase
{
    private readonly ClaimValidationOrchestrator _orchestrator;
    private readonly ILogger<ClaimsController> _logger;

    public ClaimsController(
        ClaimValidationOrchestrator orchestrator,
        ILogger<ClaimsController> logger)
    {
        _orchestrator = orchestrator;
        _logger = logger;
    }

    /// <summary>
    /// Validates a claim request using AI-powered RAG system
    /// </summary>
    /// <param name="request">The claim validation request containing policy details</param>
    /// <returns>A claim decision with approval status, reasoning, and relevant policy clauses</returns>
    /// <response code="200">Returns the claim decision</response>
    /// <response code="400">If the request is invalid</response>
    /// <response code="500">If an internal error occurs</response>
    [HttpPost("validate")]
    [ProducesResponseType(typeof(ClaimDecision), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ClaimDecision>> ValidateClaim([FromBody] ClaimRequest request)
    {
        try
        {
            _logger.LogInformation(
                "Validating claim for policy {PolicyNumber}, amount: ${Amount}",
                request.PolicyNumber,
                request.ClaimAmount
            );

            var decision = await _orchestrator.ValidateClaimAsync(request);

            _logger.LogInformation(
                "Claim validated: {PolicyNumber}, Status: {Status}, Confidence: {Confidence:F2}",
                request.PolicyNumber,
                decision.Status,
                decision.ConfidenceScore
            );

            return Ok(decision);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating claim for policy {PolicyNumber}", request.PolicyNumber);
            
            // Provide more detailed error message for common AWS issues
            var errorMessage = ex.Message;
            if (ex.InnerException != null)
            {
                errorMessage += $" Details: {ex.InnerException.Message}";
            }
            
            return StatusCode(500, new 
            { 
                error = "Internal server error during claim validation",
                details = errorMessage,
                timestamp = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Health check endpoint
    /// </summary>
    /// <returns>API health status</returns>
    /// <response code="200">API is healthy</response>
    [HttpGet("health")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Health()
    {
        return Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
    }
}
