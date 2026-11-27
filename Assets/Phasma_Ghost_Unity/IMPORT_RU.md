# Призрак для Phasma

Пакет для Unity 6.6 (6000.6) и Universal Render Pipeline. Основной FBX содержит модель в нейтральной A-позе, UV, скелет из 64 костей и скиннинг с четырьмя влияниями на вершину. Высота около 1,82 м. Пять частей модели, около 102 тыс. треугольников.

## Импорт и проверка

1. Скопируй папку `Assets/PhasmaGhost` из архива в `Assets` проекта Unity URP.
2. После импорта выбери **Phasma → Set Up Ghost (URP)**. Команда настроит Humanoid Avatar, клипы и материалы URP/Lit, затем создаст `Generated/PhasmaGhostPreview.prefab`.
3. Перетащи этот префаб в сцену со светом. В **Play** выбери его и меняй параметр **Motion** во вкладке **Parameters** окна **Animator**. Для бега и ходьбы с Root Motion оставь перед персонажем свободное место.

| Motion | Клип | Цикл | Перемещение за цикл |
| --- | --- | ---: | ---: |
| 0 | Idle | 4,00 с | 0 м |
| 1 | Walk_InPlace | 1,55 с | 0 м |
| 2 | Chase_InPlace | 0,90 с | 0 м |
| 3 | Walk_RootMotion | 1,55 с | 0,99 м |
| 4 | Chase_RootMotion | 0,90 с | 1,56 м |

In-place варианты рассчитаны на внешнее движение по ровной поверхности со скоростью **0,64 м/с** для Walk и **1,73 м/с** для Chase при скорости анимации 1. При управлении из кода выключи `Apply Root Motion`. Для клипов с перемещением включи `Apply Root Motion`. В клипах взгляд опережает небольшие повороты корпуса, а правое плечо опущено и правая рука раскачивается меньше левой.

## Если импортируешь вручную

- **Модель** — `Models/PhasmaGhost.fbx`. В **Rig** выбери **Humanoid → Create From This Model**. В **Configure** проверь зелёную карту костей. Если Unity просит калибровку, используй **Pose → Enforce T-Pose** в окне настройки Avatar. У скелета `Spine / Spine1 / Spine2` соответствуют `Spine / Chest / Upper Chest`, `UpLeg / Leg / Foot` — бедру, голени и стопе, `Arm / ForeArm / Hand` — плечу, предплечью и кисти.
- **Клипы** — каждый FBX из `Animations` содержит один цикл при 60 кадрах/с. Выбери **Humanoid → Copy From Other Avatar**, укажи Avatar модели и включи **Loop Time**. В клипах `_RootMotion` выбери кость **Root** в **Animation → Motion → Root Motion Node**. Для Idle и `_InPlace` оставь стандартный узел и включи **Bake Into Pose** для Rotation, Position (Y) и Position (XZ).
- **Материалы** — создай URP Lit для `Skin`, `Clothes`, `Shoes`, `Eyes` и `Hair`. Подставь карты с одинаковым префиксом по таблице. Для Hair включи **Alpha Clipping**, порог **0,36**, и **Render Face → Both**. Чтобы получить 1K в проекте, выбери все PNG и выставь **Max Size → 1024**.

| Карта 2048 × 2048 | Настройка текстуры | Слот URP Lit |
| --- | --- | --- |
| `*_BaseColor.png` | Default, sRGB | Base Map |
| `*_Normal.png` | Normal map | Normal Map |
| `*_Mask.png` | Default, sRGB выключен, альфа из файла | Metallic Map и Occlusion Map |

У `Mask` красный канал — metallic, зелёный — occlusion, альфа — smoothness. В материале выбери **Smoothness Source → Metallic Alpha** и множитель Smoothness **1**. Для `Hair_BaseColor` сохрани альфа-канал.

Модель и пять клипов проверены повторным импортом FBX в Blender. Настройка Avatar и воспроизведение в Unity 6.6 подлежат проверке в проекте. `Preview.jpg` показывает рендеры этой модели с разных ракурсов и кадры из клипов.

[Руководство Unity по Humanoid Avatar](https://docs.unity.com/en-us/engine/6000.6/manual/assets-and-media/asset-types/models/importing/configuringthe-avatar) · [Настройки клипов](https://docs.unity.com/en-us/engine/6000.6/manual/assets-and-media/asset-types/models/importing/class-animation-clip) · [Каналы текстур URP Lit](https://docs.unity.com/en-us/engine/6000.6/manual/materials-and-shaders/built-in/shaders-in-universalrp/channel-packed-texture)
