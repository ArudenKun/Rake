using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace Rake.Hosting;

internal sealed class EmptyHostingEnvironment : IHostEnvironment
{
    public string EnvironmentName { get; set; } = default!;
    public string ApplicationName { get; set; } = default!;
    public string ContentRootPath { get; set; } = default!;
    public IFileProvider ContentRootFileProvider { get; set; } = default!;
}
