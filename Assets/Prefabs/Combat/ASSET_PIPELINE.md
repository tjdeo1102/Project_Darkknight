# Combat Asset Pipeline

## Source imports

Raw external combat assets should be stored under:

- `Assets/ExternalAssets/Imports/Codex/CombatAnimations`
- `Assets/ExternalAssets/Imports/Codex/CombatVFX`
- `Assets/ExternalAssets/Imports/Codex/CombatSFX`
- `Assets/ExternalAssets/Imports/Codex/UIFeedback`
- `Assets/ExternalAssets/Imports/Codex/Textures`

`Assets/ExternalAssets` is ignored by git in this project, so these folders are local source-import storage.

## Runtime-ready prefabs

Project-ready combat prefabs should be created under:

- `Assets/Prefabs/Combat/VFX`
- `Assets/Prefabs/Combat/SFX`
- `Assets/Prefabs/Combat/HitFeedback`
- `Assets/Prefabs/Combat/Trails`
- `Assets/Prefabs/Combat/UIFeedback`

## Imported free assets

- Kenney Impact Sounds
  - Path: `Assets/ExternalAssets/Imports/Codex/CombatSFX/Kenney_ImpactSounds`
  - Source: https://kenney.nl/assets/impact-sounds
  - License: Creative Commons CC0
  - Contents: 130 OGG impact/foley sounds

## Recommended manual imports

- Quaternius Universal Animation Library
  - Source: https://quaternius.itch.io/universal-animation-library
  - License: Creative Commons CC0
  - Use for: humanoid combat, locomotion, hit reaction, death, retargeting tests
  - Note: itch.io requires the official free purchase/download flow.

- Mixamo animations
  - Source: https://www.mixamo.com/
  - Use for: extra sword attacks, hit reactions, rolls, death variants
  - Note: requires Adobe account and per-project license review.

- SONNISS GameAudioGDC bundle
  - Source: https://sonniss.com/gameaudiogdc/
  - Use for: heavier metal, armor, monster, ambience, and impact sound layering
  - Note: large download; import only curated clips into the project.

