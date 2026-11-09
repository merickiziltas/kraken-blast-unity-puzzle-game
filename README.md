# Kraken Blast

**A pirate-themed tap-to-blast puzzle game built with Unity and C#.** Explore a shipwreck cove, clear the deck, and claim the gold guarded by the kraken.

### [Play in your browser](https://merickiziltas.github.io/kraken-blast-unity-puzzle-game/)

<a href="https://merickiziltas.github.io/kraken-blast-unity-puzzle-game/"><img src="Preview/menu.png" alt="Kraken Blast title screen with a treasure chest and an octopus" width="240"></a> <img src="Preview/gameplay.png" alt="Gem board with a powder keg and three anchor targets" width="240">

## How to play

Tap a group of **two or more** matching gems that touch horizontally or vertically. Clear the targets before running out of moves.

| Match | Result |
| --- | --- |
| 2–3 gems | Clear the group. |
| 4–5 gems | Create a cannon that blasts along a row or column. |
| 6+ gems | Create a powder keg that clears a square area. |
| Adjacent special pieces | Tap to combine them into a larger blast. |

The ten expeditions introduce breakable jugs, anchors that need a special blast, and treasure chests that open before yielding their gold.

## Under the deck

- **Board logic** resolves connected groups with breadth-first search, tracks hits by blast source, and processes chain reactions in waves.
- **Presentation** uses separate sprite and UI components, DOTween animations, and a responsive portrait layout.
- **Level data** lives in JSON, so board layouts and move limits can be edited independently of gameplay code.

## Run in Unity

1. Clone this repository and open the project with **Unity 6000.3.10f1**.
2. Open [MainScene](Assets/_Project/Scenes/MainScene.unity) and set the Game view to **9:16**.
3. Press **Play** and select **Set sail**. To jump to an expedition, use **Kraken Blast → Set Level…** in the editor.

For a local browser build, install Unity's **Web Build Support** module and choose **Kraken Blast → Build Web Demo**. Serve the resulting **WebBuild** folder over HTTP.

## Credits

Project by **Ahmet Meriç Kızıltaş**. The pirate illustration, sprite designs, and synthesized audio were produced with AI assistance. Animation uses **DOTween** by Daniele Giardini (Demigiant). [Third-party notices](THIRD_PARTY_NOTICES.md)
