using NuGet.Versioning;

namespace Rake.Models.Messages;

public sealed record UpdateSkippedMessage(SemanticVersion Version);
