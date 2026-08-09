# Kopkari audio direction and asset shortlist

Research checked: 2026-08-04. Prices can change and may exclude tax.

## Recommendation in one sentence

Build a recognizable **Central Asian sport + modern electronic broadcast** identity, use purchased libraries for realistic horse/crowd/archery/UI effects, and commission the small number of signature music cues instead of making the whole game sound like a generic medieval or “world music” asset pack.

## What exists in the project

The current playable/build scenes and scene aliases are:

- Intro
- Home / Lobby
- AvatarCustom
- Kopkari: Registan/Registon, Jomboy/Beginer, Past Dargom, Chiroqchi
- Racing: First/Training, Zarafshan (`SecondRacing`), Egypt, Kansas, Sibir
- Planned/data-level maps: Japan and Archery mode

The current `SoundManager` has only one 2D looping room source and one UI source. It maps Intro, Home/Lobby, AvatarCustom, and every racing scene, but does **not** map Kopkari gameplay. All racing regions currently receive the same `RacingSound`.

Current Addressable audio:

- `IntroSound` -> `Assets/09.Media/IntroSound.mp3`
- `HomeSound` -> `Assets/09.Media/LobbySoundWildWest.mp3`
- `CustomRoomSound` -> `Assets/09.Media/CustomRoomSound.mp3`
- `RacingSound` -> `Assets/09.Media/RacingMusic.mp3`
- `Makarena` -> `Assets/09.Media/Makarena.wav`
- Click, Confirm, Error, PopupOpen, PopupClose, Success -> Casual Game Sounds U6

### Rights warning

Do not ship any file in `Assets/09.Media` until its invoice/license and original download source are stored in the project’s license register. In particular, `Lojay-Sarz-Ft.-Chris-Brown-Monalisa-Remix-Instrumental-Prod.-By-Sarz.mp3` names a commercial song/remix and should be removed from builds unless the project has explicit synchronization/master rights. Files such as `Makarena.wav`, `jomboy_room_sound.mp3`, and the generic downloaded MP3 names also have unclear provenance from the repository alone.

Purchasing an asset gives a license; it does not transfer copyright. Unity permits commercial use when an asset is properly licensed and embedded in the finished game, not redistributed as a raw file: [Unity Asset Store EULA FAQ](https://assetstore.unity.com/browse/eula-faq).

## Audio identity

The game should sound like a live traditional sport presented as a modern esport:

- **Cultural voice:** doira/frame drum, dombra or dutar, tanbur/rubab colors, karnay/surnay-style calls where culturally appropriate.
- **Competitive voice:** tight electronic kick, sub bass, pulse, risers, impacts, short broadcast stingers, clean UI transients.
- **Physical voice:** detailed hooves, tack, breath, cloth, dirt, mud, sand, snow, collisions, crowd reactions, objective handling.
- **Mix rule:** gameplay information wins over music. Avoid vocals during play and avoid constant “epic trailer” loudness.

One 3–5 note melodic motif should identify Kopkari across the intro, home, countdown, score, victory, and trailer. Regional maps should rearrange the motif rather than behave like unrelated games.

## Room and map music plan

| Room / map | Music direction | Ambient and local sound | Intensity behavior |
|---|---|---|---|
| Intro | 20–30 second signature: solo plucked-lute motif, ceremonial horn call, electronic rise, stadium hit | Wind, distant horse group, one clean neigh | Logo hit must resolve cleanly into Home |
| Home | 85–100 BPM restrained hybrid version of the theme; warm, premium, no vocals | Light outdoor air, distant stable/crowd texture | Low and non-fatiguing; duck under panels/tutorial voice |
| Lobby / matchmaking | 105–115 BPM pulse with muted doira and bass ostinato | Distant competitors and arena PA texture without intelligible language | Add one layer when the queue is found; stinger on ready |
| AvatarCustom | 95–110 BPM confident “locker room” groove; plucked strings plus clean electronic beat | Leather, cloth, buckle, tack, equip and color-swatch Foley | Short confirm accents; no oversized victory sounds |
| Beginner / Jomboy | Simplified 100–115 BPM theme with fewer drums | Clear tutorial cues, calm crowd, sparse horse Foley | Music automatically ducks for instructions |
| Registan Kopkari | Flagship 125–135 BPM: doira propulsion, dombra/dutar motif, horn accents, modern bass | Dense arena crowd, dry dirt hooves, cloth/tack, dust gusts | Layers for warmup, live round, carrying Uloq, last 30s, score |
| Past Dargom Kopkari | 120–128 BPM more organic/rural arrangement; hand percussion and low strings | Fields, wind, birds kept subtle, dirt/grass hoof transitions | Less stadium density; local spectators react near objectives |
| Chiroqchi Kopkari | 125–132 BPM dry steppe percussion, sharper plucked attacks, restrained synth | Dry wind, scrub, hard dirt/gravel hooves | Stronger urgency layer during a contested pickup |
| Racing Training / FirstRacing | 115–125 BPM minimal percussion/pulse | Highly readable gait, tack, breath and checkpoint cues | No melody while teaching; add layers as speed rises |
| Zarafshan / SecondRacing | 130–138 BPM Central Asian hybrid racing cue | Dirt, grass and mud gait sets; river/wind only where visible | Speed-linked percussion; final stretch adds motif/horns |
| Egypt Racing | 128–136 BPM darbuka/riq-like rhythm and oud/ney colors blended with sport electronica; avoid film clichés | Sand hooves, desert wind, cloth/tack, sparse crowd | Sand slowdown should filter/reduce music, not just lower pitch |
| Kansas Racing | 130–140 BPM hand-played guitar/banjo or harmonica color over rock/electronic drums; not comedy western | Dry prairie wind, wooden fence/ranch detail, packed dirt/grass hooves | Guitar rhythm opens up for final straight |
| Sibir Racing | 125–135 BPM cold pads, low strings/jaw-harp color and breakbeat | Snow crunch, ice scrape, cold wind, tack creak | Thin texture on ice danger; full drums return on traction |
| Japan (planned) | 132–140 BPM taiko/shamisen colors with modern bass; use a culturally informed composer | Temple/forest/city ambience only as the actual map requires | Reserve taiko accents for starts, checkpoints and final stretch |
| Archery (planned) | Sparse 90–110 BPM tension bed; silence is useful | Bow creak/draw, release, arrow flight, target impacts, crowd anticipation | Reduce music before release; result stinger only after impact |
| Results | 8–15 second win/loss/draw cues based on the same motif | Crowd swell, podium/medal ticks | Win has three sizes: round, match, championship |

## Competitive event SFX

These are more important than adding more background tracks:

| Event | Sound requirement |
|---|---|
| 3-2-1 / start | Three pitched pulses and a unique start horn/impact; readable on phone speakers |
| Uloq available | Short low horn plus localized objective shimmer; never loop loudly |
| Pickup / grip | Layer cloth pull, leather strain, body-weight thump, and a concise confirmation tone |
| Uloq stolen / dropped | Different negative cues; stolen is directional, dropped is local/world-positioned |
| Near target | Rhythmic layer or pulse, not a repeating UI beep |
| Salym / score | Immediate impact, crowd swell, 1–2 second motif; then reset cleanly |
| Last 30 / 10 seconds | Music layer plus restrained clock cue; do not beep every second until the final five |
| Stamina / health / grip warning | Separate timbres so a player can identify them without reading UI |
| Boost pickup | Short air pass, tack movement and low-frequency acceleration accent |
| Collision | Surface/body/tack layers with 6–10 variations to avoid machine-gun repetition |
| Checkpoint | Very short neutral tick; final checkpoint is brighter and larger |
| Win / loss / elimination | Separate round and match stingers; crowd side should match the result |
| Rank change / overtake | Quiet positive/negative ticks, limited by cooldown |

## Required horse and world coverage

For each player horse, support walk, trot, canter and gallop on dirt, mud, grass, sand, stone/gravel, wood, snow and ice where used. Each gait/surface cell needs at least 4 variations or well-edited seamless cycles. Add snorts, breaths at three exertion levels, neighs, landings, skids, rears, impacts, saddle, bridle, bit and leather creaks. AI horses should use distance attenuation and a strict voice limit; they must not all neigh at once.

Crowd audio should be layered as (1) constant low arena bed, (2) local excitement loops around contested objectives/final straight, and (3) one-shot reactions for start, steal, miss, score, win and loss. Avoid language-specific football chants unless they suit the setting and rights are clear.

## Purchase shortlist

### Best production-quality buys

1. **Horses Vol. 3 — $50**. 150 sounds / about 54 minutes with multiple terrains, vocalizations, harness and carriage material. This is the strongest core purchase because horse audio is heard every second of play: [A Sound Effect – Horses Vol. 3](https://www.asoundeffect.com/sound-library/horses-vol-3/). The store’s standard license is perpetual, worldwide, non-exclusive and royalty-free for sounds synchronized in games: [A Sound Effect EULA](https://www.asoundeffect.com/license-agreement/).
2. **Sports Crowd Reactions — $13.99**. Focused Unity pack for cheering, booing, clapping and chanting: [Unity Asset Store – Sports Crowd Reactions](https://assetstore.unity.com/packages/audio/sound-fx/sports-crowd-reactions-66557). Cheap and immediately useful, though audition the recordings for loop quality and unwanted language before buying.
3. **Archery Sound Effects — $19.99**. 129 WAV files covering shots, bowstring, impacts and quiver details: [Gravity Sound Studio – Archery Sound Effects](https://gravitysound.studio/products/archery-sound-effects). Buy when the Archery mode enters production.
4. **UI & Menus Sound FX Pack — $20**. 154 UI samples, commercial royalty-free license, one-time purchase: [Ovani – UI & Menus SFX](https://ovanisound.com/products/ui-menus-sound-fx-pack). Its clean signal set is more esports-appropriate than fantasy chimes. Ovani confirms perpetual commercial game use and no required credit for SFX/music: [Ovani FAQ](https://ovanisound.com/pages/faq).
5. **Hyper Action Music Pack Vol. 1 — $50**. Ten tracks supplied at three intensity levels plus short edits (50 files total), making it useful for prototyping adaptive racing and last-lap behavior: [Ovani – Hyper Action Music](https://ovanisound.com/products/hyper-action-music-pack-vol-1). Use as a temporary/secondary music library; it should not replace the custom Central Asian flagship theme.

**Production library total now:** $133.99 for Horses + Crowd + UI + Hyper Action. Add Archery later for a total of **$153.98**, before tax.

### Lower-budget alternative

- **Medieval Warfare — currently $49 sale / $59 regular** includes horse material, arrows, bows/crossbows, battle cries, Foley and ambience in one pack: [Epic Stock Media – Medieval Warfare](https://epicstockmedia.com/product/medieval-warfare-sfx-pack/). It is a cost-effective prototype/general library, but a dedicated horse library is likely to give better gait/surface continuity.
- **Bow and Arrow SFX Pack — $3.99** provides 20 draw, release, flesh and wood impact files under a royalty-free license: [Stormwave Audio on itch.io](https://stormwave-audio.itch.io/bow-and-arrow-sfx-pack). Good for early Archery implementation; omit or redesign the “flesh” impacts if the mode only uses targets.
- **Middle Eastern Music Vol. I — $15** can provide temporary Egypt music under the Standard Unity Asset Store EULA: [Unity Asset Store – Middle Eastern Music Vol. I](https://assetstore.unity.com/packages/audio/music/world/middle-eastern-music-vol-i-132488). Audition carefully; a generic fantasy-desert cue should not define the final Egypt map.

### Free, legal prototype sources

- **Sonniss GameAudioGDC archive:** more than 200 GB across yearly bundles; royalty-free commercial use, no attribution, lifetime/unlimited projects: [archive](https://sonniss.com/gameaudiogdc/) and [license](https://sonniss.com/gdc-bundle-license/). Search the tracklists for horse, hoof, saddle, leather, bow, arrow, crowd, wind, dirt, mud, sand, snow, UI, impact and whoosh. Keep the included filenames and license document.
- **Kenney Interface Sounds:** 100 CC0 UI files: [Interface Sounds](https://kenney.nl/assets/interface-sounds).
- **Kenney UI Audio:** 50 CC0 UI files: [UI Audio](https://www.kenney.nl/assets/ui-audio).
- **Kenney Impact Sounds:** 130 CC0 impact/Foley files: [Impact Sounds](https://www.kenney.nl/assets/impact-sounds).

Free libraries are excellent for prototypes and secondary layers. They still require selection, editing, loudness matching and variation management; downloading a huge bundle is not itself a finished sound design.

## Music buying decision

Do **not** buy a separate generic ethnic music pack for every region as the main soundtrack. That would fragment the brand and can turn cultural signals into stereotypes. The highest-value music spend is a composer/audio designer creating:

- one 60–90 second Home loop;
- one adaptive 3-layer flagship Kopkari cue;
- one adaptive 3-layer racing cue;
- short Intro, countdown, score, win and loss versions of the same motif;
- regional stems that swap instrumentation for Egypt, Kansas, Sibir and future Japan.

Ask for loop points, BPM/key, full mix, no-melody version, percussion/bass/melody stems, 30/60-second edits, stingers, WAV 48 kHz/24-bit masters, perpetual worldwide interactive-media rights, trailer/streaming rights, and written Content ID handling. Require the composer to disclose samples and confirm they are cleared for game redistribution.

## Implementation priorities

1. Quarantine/verify every existing `09.Media` file and create `docs/audio-license-register.csv` with filename, source URL, invoice, license, owner, purchase date and allowed uses.
2. Replace `GetRoomSoundAddress(scene.name)` with a map/game-mode audio profile so Zarafshan, Egypt, Kansas, Sibir and Kopkari do not share or miss music.
3. Separate mixer buses: Music, UI, Player Horse, Other Horses, Rider Foley, Objective, Crowd, Environment and Voice/Announcer.
4. Add two music sources for bar-synchronized crossfades and stems; keep music 2D but make gameplay/crowd/world SFX 3D.
5. Implement horse gait/surface switching, variation randomization, cooldowns and AI voice limits before adding more music.
6. Connect stingers/layers to warmup, game start, Uloq pickup/drop/steal, near target, last 30 seconds, score, final stretch, rank change, win/loss and result screen.
7. Test on phone speaker, inexpensive earbuds and headphones. The objective, warning and countdown cues must remain identifiable with music at full user setting.

## Final buy order

If budget is approved today:

1. Buy **Horses Vol. 3**.
2. Buy **Sports Crowd Reactions**.
3. Use free Kenney UI temporarily; buy **Ovani UI & Menus** only after auditioning its clean/signal subset against the current interface.
4. Use **Hyper Action Vol. 1** as adaptive-music scaffolding while commissioning the signature theme.
5. Buy **Archery Sound Effects** only when the mode has a playable production milestone.

This order spends first on sounds that directly communicate competitive play and are hardest to fake convincingly.
