# Performance budget

Status: target profile UNDEFINED; final numeric budgets NOT LOCKED.

The actual target hardware/performance profile must be defined before numeric budgets are locked. Do not invent hardware-specific FPS targets.

Required profile: platform/device, CPU/GPU/RAM, graphics API, resolution, quality tier, build configuration, power/thermal mode, target frame pacing, representative enemy/projectile/pickup counts, run duration and worst-case scenarios.

Future acceptance records CPU/GPU frame times and percentiles, spikes, allocations/GC, memory, load time and sustained thermal behavior. Select metrics and thresholds with the owner. Functional tests and performance tests have separate verdicts.

Historical Docs/gameplay/dot-verification-gates.md records an early Editor sample on i5-8350U/UHD 620 with only five enemies. This is historical evidence, not live hardware verification, a horde benchmark or performance acceptance. Standalone target-device evidence is pending.
