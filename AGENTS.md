<!-- intent-skills:start -->
## Skill Loading

Before editing files for a substantial task:
- Run `pnpm dlx @tanstack/intent@latest list` from the workspace root to see available local skills.
- If a listed skill matches the task, run `pnpm dlx @tanstack/intent@latest load <package>#<skill>` before changing files.
- Use the loaded `SKILL.md` guidance while making the change.
- Monorepos: when working across packages, run the skill check from the workspace root and prefer the local skill for the package being changed.
- Multiple matches: prefer the most specific local skill for the package or concern you are changing; load additional skills only when the task spans multiple packages or concerns.
<!-- intent-skills:end -->

## Public C# API

The package ships `TheBuilder.AppTranslations`, `TheBuilder.AppTranslations.Core` and the
`TheBuilder.AppTranslations.UmbracoAI` adapter, so their public types and members are its API. Each
of those projects declares that API in `PublicAPI.Shipped.txt` (what the last stable release
shipped) and `PublicAPI.Unshipped.txt` (changes since then). Microsoft.CodeAnalysis.PublicApiAnalyzers
fails the build when the code and the files disagree. `Directory.Build.props` turns it on for every
project that has the files.

- Make new types `internal` unless a site is meant to implement, replace, configure or consume
  them. Core's internals are visible to the main assembly and to both test projects. The main
  assembly's internals are visible to `TheBuilder.AppTranslations.Tests` only.
- Controllers are public and ASP.NET only calls public constructors, so every type a controller's
  constructor takes is public. When that type is not meant for sites, put a public interface with
  only `internal` members in front of an internal class, as `IDeliveryResponseCache` does. Nothing
  outside the assembly can implement or call it, so its members can change without breaking anyone.
  The implementing members must be `public` on the internal class.
- Adding public API: the RS0016 error quotes the exact line to add to `PublicAPI.Unshipped.txt`,
  and IDEs offer it as a code fix.
- Removing public API: when the line is in `PublicAPI.Unshipped.txt`, delete it. It never shipped,
  so nothing breaks. When the line is in `PublicAPI.Shipped.txt`, leave it there and add it to
  `PublicAPI.Unshipped.txt` with a `*REMOVED*` prefix. RS0017 names the line. This is a breaking
  change, so say so in the pull request.
- Changing a member's signature removes the old line and adds a new one. Follow both rules above.
- Keep both files in ordinal order (`LC_ALL=C sort`), with `#nullable enable` on the first line.
- Many lines at once: `dotnet format analyzers` does not apply this analyzer's fix, but each RS0016
  message is the line itself, so the build output can be collected. Build Core first, because the
  main assembly does not compile while Core fails:

  ```bash
  dotnet build src/TheBuilder.AppTranslations.Core --no-incremental 2>&1 | grep 'error RS0016' | sed -E "s/^.*Symbol '//; s/' is not part of the declared public API.*//" | LC_ALL=C sort -u
  ```

  Add the output to `PublicAPI.Unshipped.txt`, then sort the lines below `#nullable enable`.
