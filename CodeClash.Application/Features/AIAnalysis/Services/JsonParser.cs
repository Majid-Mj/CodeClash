using CodeClash.Application.Features.AIAnalysis.DTOs;
using System.Text.Json;

namespace CodeClash.Application.Features.AIAnalysis.Services
{
    public class JsonParser
    {
        public AIAnalysisResponseDto Parse(string jsonResponse)
        {
            // Clean up any potential markdown formatting from LLM
            var cleanJson = jsonResponse.Trim();
            if (cleanJson.StartsWith("```json"))
            {
                cleanJson = cleanJson.Substring(7);
            }
            else if (cleanJson.StartsWith("```"))
            {
                cleanJson = cleanJson.Substring(3);
            }

            if (cleanJson.EndsWith("```"))
            {
                cleanJson = cleanJson.Substring(0, cleanJson.Length - 3);
            }

            cleanJson = cleanJson.Trim();

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var dto = JsonSerializer.Deserialize<AIAnalysisResponseDto>(cleanJson, options);
            
            if (dto == null)
            {
                throw new System.Exception("Failed to parse AI response into the expected format.");
            }

            return dto;
        }
    }
}
