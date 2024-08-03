using System;
using AutoInterfaceAttributes;

namespace Rake.Services;

[AutoInterface]
public class SnackBarService : ISnackBarService
{
    private readonly TimeSpan _defaultDuration = TimeSpan.FromSeconds(5);
}
