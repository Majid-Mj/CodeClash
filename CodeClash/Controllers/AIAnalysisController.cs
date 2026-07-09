using CodeClash.Application.Features.AIAnalysis.Commands.AnalyzeSubmission;
using CodeClash.Application.Features.AIAnalysis.DTOs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.Tasks;

namespace CodeClash.API.Controllers
{
    [Route("api/v1/ai")]
    [ApiController]
    [Authorize]
    [EnableRateLimiting("AiAnalysisPolicy")]
    public class AIAnalysisController : ControllerBase
    {
        private readonly IMediator _mediator;

        public AIAnalysisController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost("analyze")]
        public async Task<IActionResult> Analyze([FromBody] AIAnalysisRequestDto request)
        {
            var command = new AnalyzeSubmissionCommand
            {
                SubmissionId = request.SubmissionId
            };

            var result = await _mediator.Send(command);

            return Ok(result);
        }
    }
}
