# Third-party notices

No third-party code is copied into this repository, and the library itself has no dependencies. The tests download these packages
from NuGet when they are built; their licences are as their authors publish them. Nothing from any game is included.

| Package | Licence | Why it is used |
| --- | --- | --- |
| [xunit](https://www.nuget.org/packages/xunit) | Apache-2.0 | The tests |
| [xunit.runner.visualstudio](https://www.nuget.org/packages/xunit.runner.visualstudio) | Apache-2.0 | Runs them under `dotnet test` |
| [Microsoft.NET.Test.Sdk](https://www.nuget.org/packages/Microsoft.NET.Test.Sdk) | MIT | The test host |
