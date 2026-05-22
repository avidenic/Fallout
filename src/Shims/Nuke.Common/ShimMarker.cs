// Copyright 2026 Maintainers of Fallout.
// Originally based on NUKE by Matthias Koch and contributors.
// Distributed under the MIT License.
// https://github.com/ChrisonSimtian/Fallout/blob/main/LICENSE

// Tells the TransitionShimGenerator to emit shims for every public type whose
// namespace begins with "Fallout.Common." into the corresponding "Nuke.Common."
// namespace. The generator walks all referenced Fallout.* assemblies; both
// Fallout.Common and Fallout.Build participate (FalloutBuild itself lives in
// the Fallout.Common namespace despite being declared in the Fallout.Build
// project).

[assembly: Fallout.Migrate.Shims.ShimAllPublicTypesUnder(
    fromNamespacePrefix: "Fallout.Common",
    toNamespacePrefix: "Nuke.Common")]
