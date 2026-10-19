# Narration (0.1.5)

## Reference observation

Target: installed Camtasia 26.2.2.19936 on Windows, 2026-10-19. I opened the Narration tool from the tool sidebar's **More** list, looked at the panel, and recorded nothing.

The panel, from top to bottom:

- A **microphone dropdown** (on this PC, "Microphone Array (Realtek(R) Audio)") and a wand button.
- A **level meter** bar and an **input volume slider**.
- **Mute timeline during recording**, checked by default.
- A red **Start Voice Recording** button.
- A script box with the placeholder "Paste or type script text here".
- **Generate Audio** and **Generate Script**. These are AI services and are not reproduced.

In the reference tool order, Narration sits between Audio Effects and Captions.

**Not observed:** where the reference places a recorded take on the timeline, how it times the start, the wand button's behavior, and any countdown. No reference code or assets were used. REA native UI observation is Mach-O-only.

## TatsiaCraft behavior

The Narration tool sits between Audio Effects and Captions, with the reference wording.

- **Microphone list.** Microphones come from FFmpeg's DirectShow device list (`(audio)` entries, deduplicated). While the panel is open, the selected microphone feeds a live meter: dBFS RMS over 100 ms windows, drawn green, then amber above -18 dB, then red above -6 dB. The monitor stops when you leave the tool or close the editor.
- **Input volume** (0 to 200%) scales the recording.
- **Mute timeline during recording** (on by default) mutes preview audio for the duration of the take.
- **Start Voice Recording**:
  - One FFmpeg process records 48 kHz mono PCM to `%LOCALAPPDATA%\TasiaCraft\Narration` and feeds the meter at the same time.
  - When the first level reading arrives, audio is flowing, and the timeline starts playing from the playhead.
  - The button changes to **Stop Voice Recording**.
- **Stop Voice Recording** finalizes the file. The take then goes onto a **new top track**, starting where the playhead was, as one undo checkpoint, and is selected. Clip In skips the first 100 ms reading window to line up with playback. The editor can't close while a take is recording.
- **Script box.** The script is saved with the project (up to 100,000 characters) and keeps the placeholder text.

**Still open:** the wand button and its auto-level, Generate Audio/Script, a countdown, punch-in over existing narration, measured input-latency compensation, and a choice of target track.

## Acceptance

`scripts/test-narration.ps1` runs 40 checks. A real-time FFmpeg tone source stands in for a microphone, so capture is exercised end to end.

- **Devices:** list parsing and DirectShow quoting.
- **Capture:** ready signal, meter rate and level, real-time length, input volume scaling, monitor-only mode, invalid level, a missing microphone failing.
- **Panel:** controls and wording, navigation order.
- **Script:** storage, save/reopen, length limit.
- **Recording:** mute and playback while recording, duplicate-start guard, stop unmutes and pauses.
- **Take placement:** new top track, start time, lead-in trim, duration, checkpoint and selection.
- **Export:** audio after the take's start and silence before it.
- **Other:** undo/redo, mute off, monitor stopping when you leave the tool.
