using Rake.SourceGenerators.Attributes;
using Rake.ViewModels;

namespace Rake;

[ViewModelLocator(BaseTypes = [typeof(ViewModel)])]
public sealed partial class ViewModelLocator;
