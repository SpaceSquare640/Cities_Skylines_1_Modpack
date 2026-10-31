# Cities: Skylines build references

This directory contains encrypted compilation inputs for the independently built mod branches.
It does not contain mod downloads or authorize redistribution of the underlying third-party files.
The existing project license does not replace any third-party terms.

## Container

The binary container uses AES-256-GCM with a randomly generated 256-bit key, a 12-byte nonce and
a 16-byte authentication tag. Its layout is the eight ASCII bytes CS1REF01, nonce, tag, and
ciphertext. The header is authenticated associated data. The decrypted bytes are an internal
ZIP stream containing exactly the eight files listed in manifest.json below Cities_Data/Managed/.
This internal format is unrelated to the decision to distribute mod downloads without ZIP files.

The public manifest includes only file names, sizes, assembly identities, versions and hashes.
It contains no original local directory paths. Pin this directory and the restore helper to a
reviewed full commit SHA when consuming them from another branch.

## Restore in trusted cloud builds

Use PowerShell 7.4 or newer on Windows and tools/build-references/Restore-GameReferences.ps1.
Supply CS1_GAME_REFERENCES_KEY from the repository's Actions secret only to the restore step.
Choose a fresh game-root directory below RUNNER_TEMP; point the module's game path property at it.
The helper verifies authentication, the ciphertext hash, the exact file list, sizes and hashes,
and does not overwrite existing files.

Do not run secret-bearing builds on untrusted pull-request code. A person able to modify a trusted
build script can access its decrypted references; encryption does not isolate those scripts.
Never upload the reference directory, decrypted container, or full workspace to artifacts, caches,
Git history or releases. Upload only reviewed mod output files if a later publication task allows it.
Keep the key outside source control, command-line arguments and logs.

Signature: OpenAI-Codex
