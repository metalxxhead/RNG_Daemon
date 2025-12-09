

# Install Dependencies


## Ubuntu:
sudo apt install mono-complete
sudo apt install libnewtonsoft-json5.0-cil  # or libnewtonsoft-json8.0-cil depending on version

## Arch:
pacman -S mono
https://wiki.archlinux.org/title/Mono


# Compilation Process

## From the project root:

chmod +x scripts/build.sh
chmod +x scripts/run.sh

Ubuntu:  sudo apt install libnewtonsoft-json8.0-cil
Arch:  Obtain from https://github.com/JamesNK/Newtonsoft.Json/
or install package on Ubuntu and obtain from the below path:

cp /usr/lib/cli/Newtonsoft.Json-5.0/Newtonsoft.Json.dll build/

scripts/build.sh

This produces:
build/rng.exe

scripts/run.sh


# JSON Configuration Overview (configs/rng.json)

The RNG daemon is fully controlled by a JSON config file.
You can modify patterns, device name, runtime behavior, and socket paths without recompiling.

Here is the structure:

{
  "device": {
    "name": "undefined"
  },

  "patterns": {
    "visible": ["333", "1111", "222", "434"],
    "control": {
      "salt": "C0ntrolSalt_v1",
      "hash": "52e348dda6a032c5e24a8a0c66a3e22947b99ab5c9a251dcbad85bf914ec16e1"
    }
  },

  "runtime": {
    "interval_ms": 1000,
    "window_size": 10
  },

  "sockets": {
    "unix_path": "data/rng.sock"
  },

  "logging": {
    "log_ndjson_patterns": "data/analyzer_stream_patterns.ndjson",
    "log_ndjson_control": "data/analyzer_stream_control.ndjson"
  }
}


## What Each Section Does:
device.name
- A friendly identifier included in all NDJSON output.

patterns.visible
- A list of digit patterns (e.g., "1111") the RNG engine will detect inside the sliding window.

patterns.control
- A special hidden pattern, determined via salted SHA-256 hashing.
- The RNG daemon resolves the real digits internally but never exposes them.

runtime.interval_ms
- Delay between digit generations (1000 ms = 1 digit per second).

runtime.window_size
- Length of the moving window used to detect patterns.

sockets.unix_path
- The UNIX domain socket path for real-time digit streaming.

logging.*
- NDJSON output paths for pattern, control, and summary events.

# Additional Notes

- All runtime data (NDJSON logs, socket file) is placed under the data/ directory.
- The daemon automatically deletes stale socket files and recreates them on startup.
- You can safely modify rng.json and restart the daemon with no rebuild required.

# Service Install

The RNG Daemon can be installed as a service.  Modify docs/RNG.service for your system and then just copy it to your system services directory and start/enable the service:

cp docs/RNG.service /etc/systemd/system/RNG.service

systemctl enable RNG.service
systemctl start RNG.service
systemctl status RNG.service
