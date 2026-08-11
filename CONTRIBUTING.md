# Contributing

For substantial API, architecture, or serialization changes, open an issue or
discussion before implementation so the intended contract is clear. Focused
fixes can proceed directly through a pull request.

Please note we have a code of conduct, please follow it in all your interactions
with the project.

## Package Variants

Install and contribute to one package variant at a time:

- `com.mrdav30.gridforge` is the standard Unity integration with the default
  dependency chain.
- `com.mrdav30.gridforge.lean` provides the same Unity integration without the
  `MemoryPack` dependency chain.

The variants expose overlapping assemblies and must not be installed together.

## Source Ownership

Shared Unity adapter source belongs in `Build/Base`. Edit it there; do not
hand-edit the synchronized copies under either package folder. Package
manifests, READMEs, asmdefs, plugins, and serialized sample assets remain owned
by their respective `com.mrdav30.gridforge*` package folder.

`Build/Base/CHANGELOG.md` is the canonical package changelog and syncs to both
package variants. Do not maintain separate changelog copies. Unity `.meta` files
are Unity-owned and must not be created or edited by hand.

## Pull Request Process

1. Keep changes focused and preserve package-variant boundaries.
2. Add or update Unity tests for behavior changes.
3. Add concise XML documentation for changed public runtime APIs and update the
   canonical changelog when behavior changes.
4. Sync canonical Build files into the package variants before review.
5. Exclude generated Unity, DocFX, test, and package-export artifacts from the
   pull request.

## Validation

From this repository root, run the GridForge package sync through the parent
Unity project:

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File .assets/scripts/sync-gridforge-unity-packages.ps1
```

Provide `-UnityPath` or set `UNITY_EDITOR` when Unity is not discoverable. Run
the command again when the first pass copies files; a clean sync reports
`Copied 0 files`.

Run the relevant checks from the repository root:

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File .assets/scripts/test-update-unity-package-versions.ps1
pwsh -NoProfile -ExecutionPolicy Bypass -File .assets/scripts/update-unity-package-versions.ps1 -ValidateOnly
pwsh -NoProfile -ExecutionPolicy Bypass -File .assets/scripts/test-gridforge-package-sync.ps1
pwsh -NoProfile -ExecutionPolicy Bypass -File .assets/scripts/run-gridforge-unity-editmode-tests.ps1
dotnet tool restore --tool-manifest dotnet-tools.json
dotnet tool run docfx .docs/api/docfx.json --warningsAsErrors
git diff --check
```

DocFX documents only `Build/Base/Runtime/**/*.cs`. Its generated API metadata
and site output are disposable. CI stages the required Unity and package
reference assemblies in `.docs/api/obj/references`; contributors must not add
those binary references to source control.

Authored user and maintainer guides live under `.docs/api/guides`; this
repository does not publish a GitHub Wiki. Keep the landing page, guide routes,
namespace overrides, GridForge theme, repository header link, and core-library
links aligned when changing the documentation surface. See
`.docs/api/guides/package-maintenance.md` for the complete package workflow.

The `build-and-test` workflow is the canonical validation for both package
variants, sample import, and the hosted API site. Keep DocFX configuration and
documentation changes compatible with that workflow.

## Code of Conduct

### Our Pledge

In the interest of fostering an open and welcoming environment, we as
contributors and maintainers pledge to making participation in our project and
our community a harassment-free experience for everyone, regardless of age, body
size, disability, ethnicity, gender identity and expression, level of
experience, nationality, personal appearance, race, religion, or sexual identity
and orientation.

### Our Standards

Examples of behavior that contributes to creating a positive environment
include:

- Using welcoming and inclusive language
- Being respectful of differing viewpoints and experiences
- Gracefully accepting constructive criticism
- Focusing on what is best for the community
- Showing empathy towards other community members

Examples of unacceptable behavior by participants include:

- The use of sexualized language or imagery and unwelcome sexual attention or
  advances
- Trolling, insulting/derogatory comments, and personal or political attacks
- Public or private harassment
- Publishing others' private information, such as a physical or electronic
  address, without explicit permission
- Other conduct which could reasonably be considered inappropriate in a
  professional setting

### Our Responsibilities

Project maintainers are responsible for clarifying the standards of acceptable
behavior and are expected to take appropriate and fair corrective action in
response to any instances of unacceptable behavior.

Project maintainers have the right and responsibility to remove, edit, or reject
comments, commits, code, wiki edits, issues, and other contributions that are
not aligned to this Code of Conduct, or to ban temporarily or permanently any
contributor for other behaviors that they deem inappropriate, threatening,
offensive, or harmful.

### Scope

This Code of Conduct applies both within project spaces and in public spaces
when an individual is representing the project or its community. Examples of
representing a project or community include using an official project e-mail
address, posting via an official social media account, or acting as an appointed
representative at an online or offline event. Representation of a project may be
further defined and clarified by project maintainers.

### Enforcement

Instances of abusive, harassing, or otherwise unacceptable behavior may be
reported by contacting the project team at `david.oravsky@gmail.com`. All
complaints will be reviewed and investigated and will result in a response that
is deemed necessary and appropriate to the circumstances. The project team is
obligated to maintain confidentiality with regard to the reporter of an
incident. Further details of specific enforcement policies may be posted
separately.

Project maintainers who do not follow or enforce the Code of Conduct in good
faith may face temporary or permanent repercussions as determined by other
members of the project's leadership.

### Attribution

This Code of Conduct is adapted from the [Contributor Covenant][homepage],
version 1.4, available at [http://contributor-covenant.org/version/1/4][version]

[homepage]: http://contributor-covenant.org
[version]: http://contributor-covenant.org/version/1/4/
