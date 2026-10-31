# API evidence and limitations

Signature: OpenAI-Codex_Responsible: Automated_Public_Transport_Vehicle_Management

This first implementation reads existing city bus lines only. No simulation setting is written. Compile evidence confirms signatures, not gameplay behavior.

Source references inspected in the local APTP checkout: Source/Spike/PerformanceProbe.cs (TryGetLineInfo, line census), Source/Spike/SaveRoundTripSpike.cs (vehicle chain traversal), Source/Planning/BusPrefabResolver.cs (Bus/Level1), Source/Planning/PlanningThreadingExtension.cs (simulation-frame callbacks). These are source examples, not a public cross-mod API. This module has no dependency on APTP.

Line Info may throw for removed prefabs; such records display InvalidPrefab and their transport mode cannot be confirmed. Bus plus Level1 is the current city-bus filter and still requires subscribed-game validation. Vehicle chains are bounded to 512 entries per line; longer or invalid chains become Unknown. Eight line slots are examined per simulation frame. A complete census spans multiple frames and is not an atomic or continuously refreshed snapshot. Paused simulation may delay capture. Vehicle nodes are not asserted to be independent passenger-capacity units.

CalculateTargetVehicleCount is deliberately not called: a compile-visible accessor alone does not establish safe target semantics. Target vehicles and waiting passengers remain Unknown. No capacity, demand, depot, dispatch, budget, headway, or route-control claims are made.

The .NET 3.5 mod is compiled against the pinned authenticated game references. Pure checks run under .NET Framework 4.8 and cannot establish Mono/game runtime compatibility. No private references, credentials, or decrypted assemblies are published. The reference manifest states GameVersionVerified=false; game-version compatibility requires separate verification.

Vehicle Created and m_transportLine checks also follow AI Improve EmptyVehicleAuditor.cs / TransitDwellShortenPatch.cs usage. A mismatched or uncreated chain node makes the count Unknown. No game runtime correctness claim follows from compilation.
