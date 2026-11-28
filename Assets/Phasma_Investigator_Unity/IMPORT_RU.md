# Полевой сотрудник Phasma

Первый проход персонажа для Unity 6 URP. В комплекте модель, гуманоидный скелет со скиннингом, тестовые позы и текстуры. Рост с обувью и волосами около 1,81 м. Точка глаз в нейтральной позе находится на высоте 1,70 м. В кадрах она следует за головой, поэтому при наклоне и приседе становится ниже.

## Открыть в Unity

1. Распакуйте архив. Скопируйте папку `Assets/PhasmaInvestigator` в `Assets` своего проекта. Blender для этого не нужен.
2. Откройте `Models/Phasma_Investigator.fbx` в Inspector. На вкладке **Model** задайте **Scale Factor = 1** и оставьте **Convert Units** включённым. На вкладке **Rig** выберите **Humanoid → Create From This Model → Apply**. В **Configure** проверьте кости и пальцы обеих рук. Если Unity попросит привести маппинг к T-позе, выберите **Pose → Enforce T-Pose** и сохраните Avatar.
3. На вкладке **Animation** доступны статические позы `Stand_LookDown`, `Crouch_LookDown` и `Check_Watch`. Перетащите FBX из Project в сцену для проверки.
4. В разделе **Materials** FBX назначьте материалы **Universal Render Pipeline/Lit** по таблице. Перетащите временно `WatchScreen_Sample.png` в **Base Map** материала `WatchScreen` для проверки ориентации часов.

[Импорт модели Unity 6](https://docs.unity3d.com/6000.6/Documentation/Manual/FBXImporter-Model.html) · [Настройка Humanoid Avatar](https://docs.unity3d.com/6000.6/Documentation/Manual/ConfiguringtheAvatar.html)

| Карта | Импорт текстуры | Куда назначить в URP/Lit |
|---|---|---|
| `*_BaseColor` | Default, sRGB | Base Map, цвет материала белый |
| `*_Normal` | Normal Map | Normal Map |
| `*_Mask` | Default, sRGB выключен, Alpha Source из входной текстуры | Metallic Map и Occlusion Map. В R лежит metallic, в G — AO, в A — smoothness. Источник smoothness — Metallic Alpha |
| `WatchScreen_Sample` | Default, sRGB | Base Map материала `WatchScreen` |

Карты персонажа 2048 × 2048. Для материала `Hair` включите **Alpha Clipping**, задайте **Threshold 0,36** и **Render Face = Both**. `WatchCase`, `WatchStrap` и `WatchHardware` используют однотонные материалы с отдельными параметрами гладкости. У `WatchScreen` можно назначить свою RenderTexture. UV экрана занимает весь диапазон 0–1.

## Камера и крепления

| Объект | Назначение |
|---|---|
| `Head` | Отдельный SkinnedMeshRenderer для головы, глаз и волос. Назначьте только ему слой `PlayerHead` и уберите этот слой из **Culling Mask** камеры игрока |
| `EyePoint` | Ориентир положения камеры от первого лица в иерархии головы. Направление взгляда задавайте контроллером игрока |
| `Socket_Device_R` | Крепление прибора под правой кистью |
| `Socket_Watch_L` | Крепление часов под левым предплечьем. Корпус и экран привязаны к нему |
| `WatchScreen` | Самостоятельный меш экрана с собственным материалом |

Оставьте **Optimize Game Objects** выключенным, пока проверяете камерную точку и крепления. Если используете эту оптимизацию, добавьте эти три кости в **Extra Transforms to Expose**. Положение временного фонарика в сцене показывает хват для корпуса диаметром около 34 мм.

## Открыть исходник

В `Phasma_Investigator.blend` текстуры упакованы. Камеры и тестовый фонарик находятся в коллекции `Preview_Only`. Выберите кадр в Timeline. Blender переключит позу и камеру.

| Кадр | Камера и поза |
|---|---|
| 1 | Полный рост спереди, A-поза |
| 21 | Полный рост сбоку, A-поза |
| 41 | Взгляд вниз стоя |
| 81 | Взгляд вниз в присяде |
| 121 | Взгляд на часы с фонариком в правой руке |

В первых лицах использовано вертикальное поле зрения 63°. Для отдельных кадров и роликов, снятых с внешней камеры, используйте голову из коллекции `Character`. Пол, свет и тестовый фонарик входят в сцену Blender. Все три позы в FBX статические.

См. `CREDITS.md` для сведений об исходных материалах.
