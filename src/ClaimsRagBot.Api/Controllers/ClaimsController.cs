using ClaimsRagBot.Application.RAG;
using ClaimsRagBot.Core.Interfaces;
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
    private readonly IAuditService _auditService;
    private readonly ILogger<ClaimsController> _logger;

    public ClaimsController(
        ClaimValidationOrchestrator orchestrator,
        IAuditService auditService,
        ILogger<ClaimsController> logger)
    {
        _orchestrator = orchestrator;
        _auditService = auditService;
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

    /// <summary>
    /// Search for a claim by Claim ID
    /// </summary>
    /// <param name="claimId">The unique claim identifier</param>
    /// <returns>Claim audit record if found</returns>
    /// <response code="200">Returns the claim record</response>
    /// <response code="404">If the claim is not found</response>
    [HttpGet("search/{claimId}")]
    [ProducesResponseType(typeof(ClaimAuditRecord), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClaimAuditRecord>> SearchByClaimId(string claimId)
    {
        try
        {
            _logger.LogInformation("Searching for claim: {ClaimId}", claimId);
            
            var claim = await _auditService.GetByClaimIdAsync(claimId);
            
            if (claim == null)
            {
                _logger.LogInformation("Claim not found: {ClaimId}", claimId);
                return NotFound(new { message = $"Claim {claimId} not found" });
            }

            return Ok(claim);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching for claim {ClaimId}", claimId);
            return StatusCode(500, new { error = "Error searching for claim", details = ex.Message });
        }
    }

    /// <summary>
    /// Search for claims by Policy Number
    /// </summary>
    /// <param name="policyNumber">The policy number</param>
    /// <returns>List of claims for the policy</returns>
    /// <response code="200">Returns the list of claims</response>
    [HttpGet("search/policy/{policyNumber}")]
    [ProducesResponseType(typeof(List<ClaimAuditRecord>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ClaimAuditRecord>>> SearchByPolicyNumber(string policyNumber)
    {
        try
        {
            _logger.LogInformation("Searching for claims by policy: {PolicyNumber}", policyNumber);
            
            var claims = await _auditService.GetByPolicyNumberAsync(policyNumber);
            
            return Ok(claims);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching claims for policy {PolicyNumber}", policyNumber);
            return StatusCode(500, new { error = "Error searching claims", details = ex.Message });
        }
    }

    /// <summary>
    /// Get all claims with optional status filter
    /// </summary>
    /// <param name="status">Optional status filter: Covered, Not Covered, or Manual Review</param>
    /// <returns>List of claims</returns>
    /// <response code="200">Returns the list of claims</response>
    [HttpGet("list")]
    [ProducesResponseType(typeof(List<ClaimAuditRecord>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ClaimAuditRecord>>> GetAllClaims([FromQuery] string? status = null)
    {
        try
        {
            _logger.LogInformation("Retrieving all claims with status filter: {Status}", status ?? "none");
            
            var claims = await _auditService.GetAllClaimsAsync(status);
            
            return Ok(claims);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all claims");
            return StatusCode(500, new { error = "Error retrieving claims", details = ex.Message });
        }
    }

    /// <summary>
    /// Get a single claim by ID for detailed review
    /// </summary>
    /// <param name="id">The claim ID</param>
    /// <returns>Claim details</returns>
    /// <response code="200">Returns the claim details</response>
    /// <response code="404">If the claim is not found</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ClaimAuditRecord), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClaimAuditRecord>> GetClaimById(string id)
    {
        try
        {
            _logger.LogInformation("Retrieving claim details: {ClaimId}", id);
            
            var claim = await _auditService.GetByClaimIdAsync(id);
            
            if (claim == null)
            {
                return NotFound(new { message = $"Claim {id} not found" });
            }

            return Ok(claim);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving claim {ClaimId}", id);
            return StatusCode(500, new { error = "Error retrieving claim", details = ex.Message });
        }
    }

    /// <summary>
    /// Finalize claim validation with supporting documents
    /// </summary>
    /// <param name="request">The claim finalization request with claim data and supporting document IDs</param>
    /// <returns>Final claim decision with comprehensive validation</returns>
    /// <response code="200">Returns the final claim decision</response>
    /// <response code="400">If the request is invalid</response>
    /// <response code="500">If an internal error occurs</response>
    [HttpPost("finalize")]
    [ProducesResponseType(typeof(ClaimDecision), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ClaimDecision>> FinalizeClaim([FromBody] FinalizeClaimRequest request)
    {
        try
        {
            _logger.LogInformation(
                "Finalizing claim for policy {PolicyNumber} with {DocumentCount} supporting documents",
                request.ClaimData.PolicyNumber,
                request.SupportingDocumentIds?.Count ?? 0
            );

            ClaimDecision decision;

            // Use holistic validation with supporting documents if available
            if (request.SupportingDocumentIds != null && request.SupportingDocumentIds.Any())
            {
                _logger.LogInformation(
                    "Processing claim with supporting documents: {Documents}",
                    string.Join(", ", request.SupportingDocumentIds)
                );
                
                // NEW: AI analyzes claim WITH all supporting documents
                decision = await _orchestrator.ValidateClaimWithSupportingDocumentsAsync(
                    request.ClaimData, 
                    request.SupportingDocumentIds);
                
                _logger.LogInformation(
                    "Claim validated with supporting documents - Status: {Status}, Confidence: {Confidence:F2}",
                    decision.Status,
                    decision.ConfidenceScore
                );
            }
            else
            {
                // Fallback to standard validation if no supporting documents
                _logger.LogInformation("No supporting documents provided, using standard validation");
                decision = await _orchestrator.ValidateClaimAsync(request.ClaimData);
            }

            _logger.LogInformation(
                "Claim finalized: {PolicyNumber}, Status: {Status}, Confidence: {Confidence:F2}",
                request.ClaimData.PolicyNumber,
                decision.Status,
                decision.ConfidenceScore
            );

            return Ok(decision);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finalizing claim for policy {PolicyNumber}", request.ClaimData.PolicyNumber);
            
            var errorMessage = ex.Message;
            if (ex.InnerException != null)
            {
                errorMessage += $" Details: {ex.InnerException.Message}";
            }
            
            return StatusCode(500, new 
            { 
                error = "Internal server error during claim finalization",
                details = errorMessage,
                timestamp = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Update a claim decision by specialist
    /// </summary>
    /// <param name="id">The claim ID</param>
    /// <param name="updateRequest">The update request containing new status and notes</param>
    /// <returns>Success status</returns>
    /// <response code="200">Returns success message</response>
    /// <response code="404">If the claim is not found</response>
    [HttpPut("{id}/decision")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> UpdateClaimDecision(string id, [FromBody] ClaimDecisionUpdate updateRequest)
    {
        try
        {
            _logger.LogInformation("Updating claim decision: {ClaimId} by specialist {SpecialistId}", id, updateRequest.SpecialistId);
            
            var success = await _auditService.UpdateClaimDecisionAsync(
                id,
                updateRequest.NewStatus,
                updateRequest.SpecialistNotes,
                updateRequest.SpecialistId
            );

            if (!success)
            {
                return NotFound(new { message = $"Claim {id} not found or update failed" });
            }

            return Ok(new { message = "Claim decision updated successfully", claimId = id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating claim {ClaimId}", id);
            return StatusCode(500, new { error = "Error updating claim", details = ex.Message });
        }
    }
}
