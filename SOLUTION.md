# Solution

## Decisions that differ from the brief

### .NET 10 instead of .NET 8

The brief asks for C# and .NET 8. This solution is built on .NET 10 with C# 14,
and every project targets `net10.0`. The reasons:

- **.NET 8 leaves support on 10 November 2026**, a few weeks after this
  assignment. A payments service started today on .NET 8 would need a framework
  upgrade before its first production release, and would stop receiving
  security fixes if that upgrade slipped. .NET 10 is the current long-term
  support release and is supported until November 2028.
- **The engineering standards this solution is built to fix .NET 10.** They are
  in `Claude/Standards/`, and the AI tools read them on every task. They fix the
  framework, the language version, the SDK version in `Code/global.json`, the
  XML solution format (`.slnx`), and every package version against .NET 10.
  Moving to .NET 8 would mean rewriting those standards for an older framework,
  and the point of the assignment is to show how the team would work, not a
  one-off exception to it.
- **Nothing in the brief depends on .NET 8.** The service, its container image
  and the AWS approach below are the same on .NET 10. The container image is
  built from Microsoft's .NET 10 images, which run anywhere a .NET 8 image does.

If Zapper's platform requires .NET 8, the change starts with the target
framework, the SDK version and the package versions in
`Claude/Standards/Versions.md`.

## Assumptions

To be written.

## Design and key trade-offs

To be written.

## Data model

To be written.

## AWS deployment approach

To be written.

## AI workflow and tools

To be written.

## How AI-generated work was validated

To be written.
