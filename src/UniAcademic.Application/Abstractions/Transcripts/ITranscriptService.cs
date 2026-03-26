using UniAcademic.Application.Models.Transcripts;

namespace UniAcademic.Application.Abstractions.Transcripts;

public interface ITranscriptService
{
    Task<TranscriptModel> GetTranscriptAsync(GetTranscriptQuery query, CancellationToken cancellationToken = default);
}
