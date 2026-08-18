# Acacia Custom Content Patcher

BepInEx preloader patcher for Silverpine that removes Acacia-specific custom-content editor restrictions and preserves original personality traits when an override field is blank.

Created by **Saelac and ChatGPT**.

**Current version:** 1.0.0

## Installation

Build the project and place `AcaciaCustomContentPatcher.dll` under `BepInEx/patchers/`. This is a preloader patcher, not a normal plugin DLL.

## Building

The project targets `netstandard2.0` and references `Mono.Cecil.dll` from the local BepInEx installation.
