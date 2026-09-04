namespace PixelAgent.Services;

public class SimilarityService
    : IImageSimilarityService
{
    public Task<double> Calculate(
        string design,
        string rendered)
    {
        // Similarity calculation will go here.
        return Task.FromResult(0.0);
    }
}
