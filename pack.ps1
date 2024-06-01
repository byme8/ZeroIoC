param (
    [string]$version = (Get-Date -Format "999.yyMM.ddHH.mmss")
)

dotnet clean
dotnet pack -c Release ./src/ZeroIoC.Core/ZeroIoC.Core.csproj --verbosity normal /p:Version=$version -o ./nugets
dotnet pack -c Release ./src/ZeroIoC.SourceGenerator/ZeroIoC.SourceGenerator.csproj --verbosity normal /p:Version=$version -o ./nugets
dotnet pack -c Release ./src/ZeroIoC/ZeroIoC.csproj --verbosity normal /p:Version=$version -o ./nugets
