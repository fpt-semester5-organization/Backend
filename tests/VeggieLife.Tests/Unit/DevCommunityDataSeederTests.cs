using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using VeggieLife.API.Extensions;
using VeggieLife.DAL.Context;
using Xunit;

namespace VeggieLife.Tests.Unit;

public class DevCommunityDataSeederTests
{
    [Fact]
    public void Seed_ShouldInsertVideosTranscriptsPostsCommentsAndVotes_Successfully()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: $"VeggieLife_SeedCommunityTest_{Guid.NewGuid()}")
            .Options;

        using var db = new AppDbContext(options);

        // Act
        DevCommunityDataSeeder.Seed(db);

        // Assert
        // 1. Kiểm tra 6 videos
        var videos = db.Videos.Where(v => v.VideoId >= 2101 && v.VideoId <= 2106).ToList();
        Assert.Equal(6, videos.Count);
        Assert.All(videos, v => Assert.Equal("READY", v.Status));

        // 2. Kiểm tra transcripts có segments và recipe json
        var transcripts = db.VideoTranscripts.Where(t => t.TranscriptId >= 3101 && t.TranscriptId <= 3106).ToList();
        Assert.Equal(6, transcripts.Count);
        Assert.All(transcripts, t =>
        {
            Assert.False(string.IsNullOrWhiteSpace(t.RawTranscript));
            Assert.Contains("segments", t.ExtractedRecipeJson);
            Assert.Contains("recipe", t.ExtractedRecipeJson);
            Assert.Equal("COMPLETED", t.Status);
        });

        // 3. Kiểm tra 10 bài viết chia sẻ kinh nghiệm
        var communityPosts = db.Posts.Where(p => p.PostId >= 1021 && p.PostId <= 1030).ToList();
        Assert.Equal(10, communityPosts.Count);
        Assert.All(communityPosts, p => Assert.Equal("APPROVED_ACTIVE", p.ModerationStatus));

        // 4. Kiểm tra 25 bình luận
        var comments = db.Comments.Where(c => c.CommentId >= 4001 && c.CommentId <= 4025).ToList();
        Assert.Equal(25, comments.Count);

        // 5. Kiểm tra lượt vote tích cực
        var votes = db.Votes.Where(v => v.PostId >= 1011 && v.PostId <= 1030).ToList();
        Assert.NotEmpty(votes);

        // Chạy lại lần 2 đảm bảo Idempotent
        DevCommunityDataSeeder.Seed(db);
        Assert.Equal(6, db.Videos.Count(v => v.VideoId >= 2101 && v.VideoId <= 2106));
        Assert.Equal(10, db.Posts.Count(p => p.PostId >= 1021 && p.PostId <= 1030));
    }
}

