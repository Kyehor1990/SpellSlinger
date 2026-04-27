# Repository Guidelines

## Project Structure & Module Organization

This is a Unity project for SpellSlinger. Main game content lives under `Assets/SpellSlinger/`:

- `Scripts/` contains gameplay C# MonoBehaviours and ScriptableObjects.
- `Scripts/EnemyScripts/`, `Scripts/LevelUp/`, `Scripts/Market/`, and `Scripts/Vfx/` group feature-specific logic.
- `Scenes/` contains Unity scenes such as `SampleScene.unity`, `Arda.unity`, and `Gokdeniz`-named scenes.
- `Prefabs/`, `Art/`, `Words/`, and `StatUpgrades/` contain runtime assets and data objects.
- `Assets/Plugins/Demigiant/DOTween/` and `Assets/TextMesh Pro/` are vendor/package assets; avoid editing unless upgrading those packages deliberately.

Project configuration is in `ProjectSettings/`, dependencies are in `Packages/`, and generated Unity folders such as `Library/`, `Temp/`, `Logs/`, and `UserSettings/` should not be committed.

## Build, Test, and Development Commands

- Open with Unity Editor `6000.3.9f1`, as specified in `ProjectSettings/ProjectVersion.txt`.
- `dotnet build Assembly-CSharp.csproj` checks C# compilation from the command line.
- Unity batch tests, when test assemblies exist: `Unity.exe -batchmode -projectPath . -runTests -testPlatform EditMode -testResults TestResults.xml -quit`.
- Use the Unity Editor for playtesting scenes and validating serialized prefab/scene references after script changes.

## Coding Style & Naming Conventions

Use C# conventions already present in the project: PascalCase for types, methods, properties, and enum values; camelCase for fields and local variables. Keep MonoBehaviour lifecycle methods (`Awake`, `OnEnable`, `Update`, `FixedUpdate`) private unless Unity or other code needs public access. Prefer `[SerializeField] private` for inspector-tuned values instead of public fields when adding new code. Keep one primary type per `.cs` file and name the file after the type.

## Testing Guidelines

The Unity Test Framework package is installed, but no dedicated test folders are currently present. Add Edit Mode tests under `Assets/Tests/EditMode/` for pure logic and Play Mode tests under `Assets/Tests/PlayMode/` for scene, prefab, input, or physics behavior. Name test files after the system under test, for example `SentenceManagerTests.cs`.

## Commit & Pull Request Guidelines

Recent commits use short, descriptive messages, sometimes in Turkish, such as `Sentence UI Feedback` and `Modifier Kelimeleri eklendi`. Keep commits focused on one feature or fix. Pull requests should include a brief summary, affected scenes/prefabs/scripts, test or playtest notes, and screenshots or clips for visible UI, VFX, or gameplay changes. Mention any required Unity package changes.

## Agent-Specific Instructions

Preserve `.meta` files when moving or adding Unity assets. Do not hand-edit generated project files unless Unity regeneration is not enough. Avoid unrelated formatting churn in vendor assets and serialized `.unity`, `.prefab`, or `.asset` files.
