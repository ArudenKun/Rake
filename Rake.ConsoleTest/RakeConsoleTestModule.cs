using Rake.Core;
using Volo.Abp.Autofac;
using Volo.Abp.Modularity;

namespace Rake.ConsoleTest;

[DependsOn(typeof(RakeCoreModule), typeof(AbpAutofacModule))]
public class RakeConsoleTestModule : AbpModule { }
