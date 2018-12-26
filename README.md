QuickInject
===========

QuickInject is an opinionated dependency injection container. It only supports constructor injection. Furthermore it generates `unsafe code`.

#### Single Goal

 * Generate efficient code while supporting two features: *Lifetime Managers* and *Child Containers*.

#### Non Goals

 * Safe code
 * Factories
 * Supporting ASP.NET Core MVC (this may change in the future)
 * `Func<>` and `Lazy<>` of unregistered types.
 * Open Generics (required by ASP.NET Core)
 * Named registrations ([Marker interface pattern](https://en.wikipedia.org/wiki/Marker_interface_pattern) can be used instead)

####

## Microsoft Open Source Code of Conduct

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/). For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.