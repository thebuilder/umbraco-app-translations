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

`TheBuilder.AppTranslations` and `TheBuilder.AppTranslations.Core` both ship in the package, so
their public types and members are the package's API. Each project declares that API in
`PublicAPI.Shipped.txt` (what a stable release has shipped) and `PublicAPI.Unshipped.txt` (changes
since then), and Microsoft.CodeAnalysis.PublicApiAnalyzers fails the build when the code and the
files disagree.

- Make new types `internal` unless a site is meant to implement, replace, configure or consume
  them. Both test projects can see internals.
- Adding public API: the RS0016 error quotes the exact line to add to `PublicAPI.Unshipped.txt`,
  and IDEs offer it as a code fix.
- Removing or changing public API: delete the line from `PublicAPI.Unshipped.txt`, or, when it is in
  `PublicAPI.Shipped.txt`, add it to `PublicAPI.Unshipped.txt` with a `*REMOVED*` prefix. Either way
  it is a breaking change, so say so in the pull request.
- Keep both files sorted, with `#nullable enable` on the first line.
