# RNG Daemon (v3.0)

A lightweight, high-quality random number generator daemon designed for real-time pattern analysis experiments.
This project streams RNG output over a UNIX socket, logs structured NDJSON events, and supports user-defined pattern sets through a simple JSON config.

The daemon is built with C# / Mono and runs on Linux systems with minimal dependencies.
All installation, configuration, and usage instructions are provided in the docs/ directory.

## Features

- Cryptographically secure random digit generation
- Sliding window pattern detection
- Hidden control-pattern support for blind statistical studies
- Real-time digit streaming via UNIX socket
- Structured NDJSON logging for offline analysis
- Fully configurable device name, patterns, timing, and output paths

## Documentation
See the docs/ folder for setup, configuration, and analyzer integration guides.
