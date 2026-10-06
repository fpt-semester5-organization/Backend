using System;
using System.Collections.Generic;

namespace VeggieLife.DAL.Entities;

public partial class VideoTranscript
{
    public int TranscriptId { get; set; }

    public int VideoId { get; set; }

    public string? RawTranscript { get; set; }

    public string? ExtractedRecipeJson { get; set; }

    public string? SttModelVersion { get; set; }

    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public virtual Video Video { get; set; } = null!;
}
