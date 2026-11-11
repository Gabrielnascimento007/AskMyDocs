using AskMyDocs.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AskMyDocs.Api.Infrastructure.Persistence.Configurations;

public sealed class ChunkConfiguration : IEntityTypeConfiguration<Chunk>
{
    public const int EmbeddingDimensions = 1536;

    public void Configure(EntityTypeBuilder<Chunk> builder)
    {
        builder.ToTable("chunks");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Content).IsRequired();
        builder.Property(c => c.Embedding)
            .HasColumnType($"vector({EmbeddingDimensions})")
            .IsRequired();

        builder.HasIndex(c => c.DocumentId);
        builder.HasIndex(c => c.Embedding)
            .HasMethod("hnsw")
            .HasOperators("vector_cosine_ops");
    }
}
