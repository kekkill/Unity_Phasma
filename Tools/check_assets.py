"""Static Unity asset checks only; does not compile C# or execute Unity physics/navigation.
Run: PYTHONPATH=/tmp/phasma-syntax-tools python Tools/check_assets.py
"""
from pathlib import Path
import json
import re
import struct
import wave
import yaml
from tree_sitter import Language, Parser
import tree_sitter_c_sharp

root = Path(__file__).resolve().parents[1]
assets = root / 'Assets'
parser = Parser(Language(tree_sitter_c_sharp.language()))
sources = list(assets.rglob('*.cs'))
for path in sources:
    assert not parser.parse(path.read_bytes()).root_node.has_error, f'C# syntax: {path}'
    assert path.with_suffix(path.suffix + '.meta').exists(), f'Missing metadata: {path}'
guid_format = re.compile(r"[0-9a-f]{32}")
guids = {}
for path in assets.rglob('*.meta'):
    match = re.search(r'^guid: (\S+)$', path.read_text(), re.M)
    assert match and guid_format.fullmatch(match[1]), f'Invalid metadata GUID: {path}'
    assert match[1] not in guids, f'Duplicate GUID: {path}, {guids.get(match[1])}'
    guids[match[1]] = path

def scene_objects(path):
    text = path.read_text()
    for match in re.finditer(r'\bguid:[ \t]*([^,}\s]*)', text):
        assert guid_format.fullmatch(match[1]), f'Invalid scene GUID {match[1]!r}: {path}'
    objects = {}
    for block in re.split(r'(?=^--- !u!)', text, flags=re.M)[1:]:
        fid = int(re.search(r'&(\d+)', block)[1])
        assert fid not in objects, f'Duplicate object {fid}: {path}'
        objects[fid] = yaml.safe_load(block.split('\n', 1)[1])
    for value in objects.values():
        def references(item):
            if isinstance(item, dict):
                if 'fileID' in item and 'guid' not in item:
                    assert item['fileID'] == 0 or item['fileID'] in objects, f'Broken local reference {item}: {path}'
                for child in item.values(): references(child)
            elif isinstance(item, list):
                for child in item: references(child)
        references(value)
    for fid, obj in objects.items():
        if 'GameObject' in obj:
            for component in obj['GameObject']['m_Component']:
                data = next(iter(objects[component['component']['fileID']].values()))
                assert data['m_GameObject']['fileID'] == fid, f'Component owner mismatch: {path}/{fid}'
    return objects

for scene in ['Prototype', 'RoomSearchPrototype', 'ArchiveInvestigation']:
    objects = scene_objects(assets / f'Scenes/{scene}.unity')
    def mono(fid): return objects[fid]['MonoBehaviour']
    for fid, cls in [(513613271, 'GhostHidingKnowledge'), (513613272, 'GhostHearing'), (513613273, 'GhostHidingInvestigation')]:
        data = mono(fid)
        assert data['m_GameObject']['fileID'] == 513613253
        assert data['m_EditorClassIdentifier'] == 'Assembly-CSharp::' + cls
    pickup_guid = re.search('guid: (\\w+)', (assets / 'Scripts/Items/PickupItem.cs.meta').read_text())[1]
    noise_guid = re.search('guid: (\\w+)', (assets / 'Scripts/Items/ThrownItemNoise.cs.meta').read_text())[1]
    item_audio_guid = re.search('guid: (\\w+)', (assets / 'Scripts/Audio/ItemAudio.cs.meta').read_text())[1]
    door_guid = re.search('guid: (\\w+)', (assets / 'Scripts/World/Door.cs.meta').read_text())[1]
    door_audio_guid = re.search('guid: (\\w+)', (assets / 'Scripts/Audio/DoorAudio.cs.meta').read_text())[1]
    components = [obj['MonoBehaviour'] for obj in objects.values() if 'MonoBehaviour' in obj]
    for pickup in [data for data in components if data['m_Script']['guid'] == pickup_guid]:
        assert any(data['m_GameObject'] == pickup['m_GameObject'] and data['m_Script']['guid'] == noise_guid for data in components)
        assert any(data['m_GameObject'] == pickup['m_GameObject'] and data['m_Script']['guid'] == item_audio_guid for data in components)
    for door in [data for data in components if data['m_Script']['guid'] == door_guid]:
        assert any(data['m_GameObject'] == door['m_GameObject'] and data['m_Script']['guid'] == door_audio_guid for data in components)
    if scene == 'RoomSearchPrototype':
        zone = mono(65000003)
        assert zone['door']['fileID'] == 65000102 and zone['approachPoint']['fileID'] == 65000051
        assert objects[65000002]['BoxCollider']['m_IsTrigger'] == 1
        nav = mono(1971981892)
        assert {65000014, 65000024, 65000034, 65000044}.issubset({ref['fileID'] for ref in nav['geometry']})
        assert {1556033313, 65000114, 66000014} == {ref['fileID'] for ref in nav['movingObstacles']}
        assert mono(65000102)['initiallyOpen'] == 0 and mono(65000102)['openAngle'] == -90
        # Closet root/body remain inside the Bedroom's existing six-metre footprint.
        position = objects[65000001]['Transform']['m_LocalPosition']
        assert -8 < position['x'] - 1.075 and position['x'] + 1.075 < -2
        assert -3 < position['z'] - 1.075 and position['z'] + 1.075 < 3
        # Stationary seal and rotating leaf have different parents: opening cannot rotate the seal away.
        assert mono(66000002)['leafPivot']['fileID'] == 66000031 and mono(66000002)['initiallyOpen'] == 1
        assert objects[66000021]['Transform']['m_Father']['fileID'] == 66000001
        assert objects[66000011]['Transform']['m_Father']['fileID'] == 66000031
        barrier = objects[66000022]['BoxCollider']
        assert barrier['m_Enabled'] == 0 and barrier['m_IsTrigger'] == 0
        assert barrier['m_Size']['x'] >= 2 and barrier['m_Size']['y'] >= 3
        assert mono(66000102)['entrance']['fileID'] == 66000002 and mono(66000102)['exitBarrier']['fileID'] == 66000022
        assert mono(66000102)['session']['fileID'] == 50010002
        assert {ref['fileID'] for ref in mono(66000102)['roomLights']} == {2017521005, 60010006}
        assert mono(513613274)['hunt']['fileID'] == 513613261
        assert mono(1277915033)['m_GameObject']['fileID'] == 1277915010
        for reference in [mono(66000202)['profile'], mono(513613261)['sequenceProfile']]:
            assert reference['guid'] in guids and reference['fileID'] == 11400000
    print(f'{scene}: {len(objects)} unique objects; local references, owners and sound/hiding wiring passed.')

# The front end is a separate build scene; all mission choices are authored, not Inspector TODOs.
menu = scene_objects(assets / 'Scenes/MainMenu.unity')
catalog_guid = re.search(r'^guid: (\w+)', (assets / 'Settings/MissionPreparation.asset.meta').read_text(), re.M)[1]
assert menu[1002]['MonoBehaviour']['catalog']['guid'] == catalog_guid
assert sum('Camera' in obj for obj in menu.values()) == 1
assert sum('AudioListener' in obj for obj in menu.values()) == 1
assert menu[330585546]['Transform']['m_Father']['fileID'] == 0
main = scene_objects(assets / 'Scenes/RoomSearchPrototype.unity')
setup = main[50010005]['MonoBehaviour']
assert setup['catalog']['guid'] == catalog_guid
spawns = setup['equipment']
assert {row['id'] for row in spawns} == set(range(6)) and len(spawns) == 6
expected_items = ['FlashLight', 'Thermometer', 'EMFReader', 'UVFlashlight', 'GhostWritingBook', 'MotionSensor']
for row in spawns:
    item = main[row['item']['fileID']]['GameObject']
    assert item['m_Name'] == expected_items[row['id']] and item['m_IsActive'] == 1
assert main[331973595]['GameObject']['m_IsActive'] == 0, 'Do not resurrect the old ward.'
for fid in [513613254, 50020002]:
    assert main[fid]['MonoBehaviour']['missionSetup']['fileID'] == 50010005
power = main[68000005]['MonoBehaviour']
assert power['initiallyPowered'] == 0 and power['indicator']['fileID'] == 68000013
for fid in [2017521005, 60010006]:
    switch = main[fid]['MonoBehaviour']
    assert switch['circuit']['fileID'] == 68000005
    assert main[switch['targetLight']['fileID']]['Light']['m_Enabled'] == 1, 'Logical initial switch position must survive circuit startup.'
assert 68000004 in {ref['fileID'] for ref in main[1971981892]['MonoBehaviour']['geometry']}
assert main[50010004]['MonoBehaviour']['buildingPower']['fileID'] == 68000005
assert main[68000022]['Light']['m_Enabled'] == 1

def asset_data(path): return yaml.safe_load(path.read_text().split('\n', 3)[3])['MonoBehaviour']
catalog = asset_data(assets / 'Settings/MissionPreparation.asset')
maps = [option['sceneName'] for option in catalog['maps']]
assert maps == ['RoomSearchPrototype', 'ArchiveInvestigation']
assert all(option['displayName'] and option['description'] for option in catalog['maps'])
assert len(catalog['equipment']) == 8 and {option['id'] for option in catalog['equipment']} == set(range(8))
assert len(catalog['difficulties']) == 3
for reference in catalog['difficulties']:
    profile = asset_data(Path(str(guids[reference['guid']])[:-5]))
    for field in ['huntSpeedMultiplier', 'huntDurationMultiplier', 'huntCheckIntervalMultiplier']:
        assert profile[field] > 0
normal_path = Path(str(guids[catalog['defaultDifficulty']['guid']])[:-5])
assert normal_path.name == 'DifficultyNormal.asset'
normal = asset_data(normal_path)
assert all(value == 1 for key, value in normal.items() if key.endswith('Multiplier'))
build = yaml.safe_load((root / 'ProjectSettings/EditorBuildSettings.asset').read_text().split('\n', 3)[3])['EditorBuildSettings']['m_Scenes']
assert build[0]['path'] == 'Assets/Scenes/InvestigationHub.unity' and build[0]['enabled'] == 1
for scene in build:
    if not scene['enabled']: continue
    assert (root / scene['path']).exists()
    assert guids[scene['guid']] == root / (scene['path'] + '.meta')
print(f'MainMenu: {len(menu)} objects; build order, difficulty/catalog, equipment and power references passed.')

enabled = {Path(scene['path']).stem for scene in build if scene['enabled']}
assert set(maps + ['InvestigationHub', 'MainMenu']).issubset(enabled)
hub = scene_objects(assets / 'Scenes/InvestigationHub.unity')
assert hub[1002]['MonoBehaviour']['isHub'] == 1
assert hub[1002]['MonoBehaviour']['catalog']['guid'] == catalog_guid
assert sum('Camera' in obj for obj in hub.values()) == 1
assert any(obj.get('GameObject', {}).get('m_Name') == 'Planning Desk' for obj in hub.values())
phone = hub[74000021]['MonoBehaviour']
assert phone['m_EditorClassIdentifier'] == 'Assembly-CSharp::HubPhone'
assert phone['menu'] == {'fileID': 1002}
body = hub[phone['phoneBody']['fileID']]['BoxCollider']
assert body['m_GameObject'] == phone['m_GameObject'] and body['m_Enabled'] and not body['m_IsTrigger']
reader = hub[phone['playerInput']['fileID']]['MonoBehaviour']
assert reader['m_EditorClassIdentifier'] == 'Assembly-CSharp::PlayerInputReader'
assert reader['inputActions']['guid'] in guids
assert hub[330585546]['Transform']['m_Father'] == {'fileID': 1277915012}
assert hub[1277915014]['MonoBehaviour']['headTransform'] == {'fileID': 330585546}
hub_crouch = hub[1277915019]['MonoBehaviour']
assert hub_crouch['m_EditorClassIdentifier'] == 'Assembly-CSharp::PlayerCrouch'
assert hub_crouch['m_GameObject'] == {'fileID': 1277915010}
assert hub_crouch['headTransform'] == hub[1277915014]['MonoBehaviour']['headTransform']
assert hub_crouch['crouchingHeadY'] < hub_crouch['standingHeadY']
assert phone['playerCamera'] == {'fileID': 330585545}
assert all(hub[i]['BoxCollider']['m_Enabled'] for i in range(74000000, 74000010))
assert phone['preparationArrival'] == {'fileID': 75003101}
assert 'hubArrival' not in phone
assert 75001035 not in hub and hub[75001034]['BoxCollider']['m_Enabled']
arrival = hub[75003101]['Transform']['m_LocalPosition']
assert 18.5 < arrival['x'] < 29.5 and -4.5 < arrival['z'] < 4.5 and arrival['y'] > 1
assert all(hub[75000004 + 10*i]['BoxCollider']['m_Enabled'] for i in range(6))
for kind in range(3):
    base = 75001000 + 10*kind
    station = hub[base+5]['MonoBehaviour']
    assert station['kind'] == kind and station['prompt']
    assert station['m_GameObject'] == {'fileID': base}
    assert hub[base+4]['BoxCollider']['m_GameObject'] == station['m_GameObject']
    assert hub[base+4]['BoxCollider']['m_Enabled'] and not hub[base+4]['BoxCollider']['m_IsTrigger']

ring_path = Path(str(guids[phone['ring']['guid']])[:-5])
with wave.open(str(ring_path), 'rb') as stream:
    rate = stream.getframerate()
    assert stream.getnframes() == rate * 2 and stream.getnchannels() == 1 and stream.getsampwidth() == 2
    ring = struct.unpack('<' + 'h' * (rate * 2), stream.readframes(rate * 2))
    assert max(map(abs, ring[:rate // 2])) > 1000
    assert not any(ring[rate // 2:rate * 9 // 10])
    assert max(map(abs, ring[rate:rate * 14 // 10])) > 1000
    assert not any(ring[rate * 15 // 10:])
print('Hub player, preparation arrival, three preparation stations, colliders and two ring bursts passed.')


archive = scene_objects(assets / 'Scenes/ArchiveInvestigation.unity')
def archive_component(fid): return archive[fid]['MonoBehaviour']
for fid in [50010002, 50010003, 50010004, 50010005, 50030063, 66000102]:
    assert archive_component(fid)['m_Enabled'] == 1
archive_setup = archive_component(50010005)
assert archive_setup['equipment'] == setup['equipment']
assert archive_setup['carriedInventory'] == {'fileID': 1277915030}
assert len(archive_setup['reservePositions']) >= len(catalog['equipment'])
assert [option.get('carriedSlots', 0) for option in catalog['maps']] == [0, 3]

route = archive_component(75500001)
assert route['m_EditorClassIdentifier'] == 'Assembly-CSharp::ArchiveMissionRoute'
assert route['workroom'] == {'fileID': 50030063}
assert route['session'] == archive_component(66000102)['session'] == {'fileID': 50010002}
assert archive_component(50010004)['archiveRoute'] == {'fileID': 75500001}
assert archive_component(50010004)['safeZone'] == {'fileID': 75400003}
assert archive_component(50030055)['route'] == {'fileID': 75500001}
assert route['entrance'] == {'fileID': 66000002}
assert route['entranceBarrier'] == {'fileID': 66000022}
assert not archive_component(66000002)['initiallyOpen'] and not archive_component(66000002)['allowGhostInteraction']
assert archive_component(66000102)['entrance'] == archive_component(66000102)['exitBarrier'] == {'fileID': 0}
assert archive[route['entryThreshold']['fileID']]['BoxCollider']['m_IsTrigger']
assert archive[1277915012]['Transform']['m_LocalPosition']['z'] < -3
assert archive[route['curb']['fileID']]['Transform']['m_LocalPosition']['z'] < -3
clip = Path(str(guids[route['dispatchReply']['guid']])[:-5])
with wave.open(str(clip), 'rb') as stream:
    assert (stream.getnchannels(), stream.getsampwidth(), stream.getframerate()) == (1, 2, 24000)
    assert stream.getframerate() < stream.getnframes() < 5 * stream.getframerate()

rooms = {ref['fileID'] for ref in archive_component(513613254)['rooms']}
expected_ring = {
    50001003: {1314804767, 1723003169},
    1314804767: {50001003, 71000101},
    1723003169: {50001003, 71000101},
    71000101: {1314804767, 1723003169, 75200101},
    75200101: {71000101},
}
assert rooms == set(expected_ring)
for fid, connected in expected_ring.items():
    room = archive_component(fid)
    assert room['checkHeight']
    assert {ref['fileID'] for ref in room['connectedRooms']} == connected

nav = archive_component(1971981892)
nav_geometry = {ref['fileID'] for ref in nav['geometry']}
assert {50030012, 75300034, 75300044}.isdisjoint(nav_geometry)
assert {75000154, 75200004, 75000094}.issubset(nav_geometry)
assert {'fileID': 72000114} in nav['movingObstacles']
assert archive_component(72000003)['door'] == {'fileID': 72000102}
assert {'fileID': 71010006} in archive_component(66000102)['roomLights']
assert archive_component(71010006)['circuit'] == {'fileID': 68000005}
basement_switch = archive_component(75200302)
assert basement_switch['circuit'] == {'fileID': 68000005}
assert archive[basement_switch['targetLight']['fileID']]['Light']['m_GameObject'] == {'fileID': 75200200}
assert {'fileID': 75200302} in archive_component(66000102)['roomLights']
assert {ref['fileID'] for ref in nav['additionalSaltFloors']} <= nav_geometry


def extent_x(fid):
    transform = archive[fid]['Transform']
    center = transform['m_LocalPosition']['x']
    half = transform['m_LocalScale']['x'] / 2
    return center - half, center + half

west, middle, east = map(extent_x, (2127530396, 75000071, 71000001))
assert abs(middle[0] - west[1] - 2) < 1e-6
assert abs(east[0] - middle[1] - 2) < 1e-6
assert archive[75200001]['Transform']['m_LocalPosition']['y'] == -3.3
# The slab must close the storage side and both landings while leaving the stair shaft open.
ceiling = [archive[fid]['Transform'] for fid in (75500201, 75500211, 75500221)]
for fid in (75500204, 75500214, 75500224):
    assert archive[fid]['BoxCollider']['m_Enabled'] and fid not in nav_geometry

def ceiling_above(x, z):
    return any(
        abs(t['m_LocalPosition']['x'] - x) <= t['m_LocalScale']['x'] / 2
        and abs(t['m_LocalPosition']['z'] - z) <= t['m_LocalScale']['z'] / 2
        for t in ceiling
    )

assert all(ceiling_above(x, z) for x,z in [(1, 1.1), (5, 9.8), (7.8, 1.1), (7.8, 9.8)])
assert not ceiling_above(6.8, 6.0)

ramp = archive[75000151]['Transform']
assert 25 < -ramp['m_LocalEulerAnglesHint']['x'] < 40
assert archive_component(75500120)['viewpoints'] == [{'fileID': 75500101}, {'fileID': 75500111}]

ghost_visual = archive_component(75500520)
assert ghost_visual['m_EditorClassIdentifier'] == 'Assembly-CSharp::GhostModelVisual'
assert ghost_visual['m_GameObject'] == {'fileID': 513613253}
assert archive_component(513613265)['visible'], 'Keep the debug silhouette available as fallback.'
ghost_assets = assets / 'PhasmaGhost'
map_slots = {'_BaseMap': 'BaseColor', '_BumpMap': 'Normal', '_MetallicGlossMap': 'Mask', '_OcclusionMap': 'Mask'}
for name in ('Skin', 'Clothes', 'Shoes', 'Eyes', 'Hair'):
    mat_ref = ghost_visual[name.lower()]
    path = ghost_assets / 'Materials' / f'{name}.mat'
    assert mat_ref['fileID'] == 2100000 and guids[mat_ref['guid']] == path.with_suffix('.mat.meta')
    material_text = path.read_text().split('--- !u!21 &2100000\n', 1)[1]
    maps = yaml.safe_load(material_text)['Material']['m_SavedProperties']['m_TexEnvs']
    for slot, suffix in map_slots.items():
        ref = next(mapping[slot]['m_Texture'] for mapping in maps if slot in mapping)
        assert ref['fileID'] == 2800000
        assert guids[ref['guid']] == ghost_assets / 'Textures' / f'{name}_{suffix}.png.meta'
    for suffix in ('BaseColor', 'Normal', 'Mask'):
        texture_importer = yaml.safe_load((ghost_assets / 'Textures' / f'{name}_{suffix}.png.meta').read_text())['TextureImporter']
        assert texture_importer['textureType'] == (1 if suffix == 'Normal' else 0)
        assert texture_importer['mipmaps']['sRGBTexture'] == (1 if suffix == 'BaseColor' else 0)
        android = next(platform for platform in texture_importer['platformSettings'] if platform['buildTarget'] == 'Android')
        assert android['overridden'] and android['maxTextureSize'] == 1024
runtime_ghost = ghost_assets / 'Resources/PhasmaGhost'
ghost_fbx = [runtime_ghost / 'PhasmaGhost.fbx']
ghost_fbx += [runtime_ghost / 'Animations' / f'PhasmaGhost@{clip}.fbx' for clip in ('Idle', 'Walk_InPlace', 'Chase_InPlace')]
ghost_fbx += [ghost_assets / 'RootMotion' / f'PhasmaGhost@{clip}.fbx' for clip in ('Walk_RootMotion', 'Chase_RootMotion')]
for path in ghost_fbx:
    with path.open('rb') as stream:
        assert stream.read(18) == b'Kaydara FBX Binary', path
    assert path.with_suffix('.fbx.meta').exists()
print('Archive ghost attachment, five imported materials and six FBX assets passed.')

assert ghost_visual['appearance'] == 2, 'Archive should select an appearance per run.'
izlom_assets = assets / 'Izlom'
appearance_ref = ghost_visual['izlomAppearance']
appearance_path = izlom_assets / 'IzlomAppearance.asset'
assert appearance_ref['fileID'] == 11400000
assert guids[appearance_ref['guid']] == appearance_path.with_suffix('.asset.meta')
appearance = yaml.safe_load(appearance_path.read_text().split('\n', 3)[3])['MonoBehaviour']
assert guids[appearance['m_Script']['guid']] == assets / 'Scripts/Ghost/GhostAppearance.cs.meta'
assert appearance['modelResource'] == 'Izlom/Izlom' and appearance['walkSpeed'] == 0.6
model = izlom_assets / 'Resources' / f"{appearance['modelResource']}.fbx"
with model.open('rb') as stream:
    assert stream.read(18) == b'Kaydara FBX Binary', model
assert model.with_suffix('.fbx.meta').is_file()
material = izlom_assets / 'IzlomSurface.mat'
assert appearance['material']['fileID'] == 2100000
assert guids[appearance['material']['guid']] == material.with_suffix('.mat.meta')
material_text = material.read_text().split('--- !u!21 &2100000\n', 1)[1]
izlom_maps = yaml.safe_load(material_text)['Material']['m_SavedProperties']['m_TexEnvs']
for slot, suffix in map_slots.items():
    ref = next(mapping[slot]['m_Texture'] for mapping in izlom_maps if slot in mapping)
    texture_name = 'URP_Mask' if suffix == 'Mask' else suffix
    texture = izlom_assets / 'Textures' / f'Izlom_{texture_name}.png'
    metadata = texture.with_suffix('.png.meta')
    assert ref['fileID'] == 2800000 and guids[ref['guid']] == metadata
    assert struct.unpack('>II', texture.read_bytes()[16:24]) == (2048, 2048)
    importer = yaml.safe_load(metadata.read_text())['TextureImporter']
    assert importer['textureType'] == (1 if suffix == 'Normal' else 0)
    assert importer['mipmaps']['sRGBTexture'] == (1 if suffix == 'BaseColor' else 0)
    if suffix == 'Mask':
        assert importer['alphaUsage'] == 1
    android = next(platform for platform in importer['platformSettings'] if platform['buildTarget'] == 'Android')
    assert android['overridden'] and android['maxTextureSize'] == 1024
print('Archive appearance selection, Izlom FBX and URP atlas references passed.')


board = archive_component(75500505)
assert board['m_GameObject'] == {'fileID': 75500500}
assert board['mission'] == {'fileID': 50010005} and board['breaker'] == {'fileID': 68000001}
board_material, = archive[75500503]['MeshRenderer']['m_Materials']
assert board_material['fileID'] == 2100000
assert guids[board_material['guid']] == assets / 'ArchivePresentation/ArchiveWallPlan.mat.meta'

assert archive[75500504]['BoxCollider']['m_Enabled'] and not archive[75500504]['BoxCollider']['m_IsTrigger']
for name, count in [('Easy', 3), ('Normal', 2), ('Hard', 1)]:
    assert asset_data(assets / f'Settings/Difficulty{name}.asset')['equipmentMapHints'] == count
assert [hint['reserveIndex'] for hint in board['hints']] == [3, 4, 0]
assert [hint['floor'] for hint in board['hints']] == [1, 0, 1]
assert [floor['title'] for floor in board['floors']] == ['Подвал', 'Первый этаж']
for hint in board['hints']:
    position = archive_setup['reservePositions'][hint['reserveIndex']]
    collider = archive[hint['room']['fileID']]['BoxCollider']
    owner = archive[collider['m_GameObject']['fileID']]['GameObject']
    transform = archive[owner['m_Component'][0]['component']['fileID']]['Transform']
    for axis in ('x', 'y', 'z'):
        center = transform['m_LocalPosition'][axis] + collider['m_Center'][axis]
        assert abs(position[axis] - center) < collider['m_Size'][axis] / 2

breath = archive_component(75500510)
assert breath['m_GameObject'] == {'fileID': 1277915010} and breath['coldThreshold'] == 7
textures = [(floor['plan'], (864, 576)) for floor in board['floors']]
textures += [(board['icons'], (576, 64)), (breath['vapor'], (128, 92))]
for reference, dimensions in textures:
    assert reference['fileID'] == 2800000
    texture = Path(str(guids[reference['guid']])[:-5]).read_bytes()
    assert texture[:8] == b'\x89PNG\r\n\x1a\n' and struct.unpack('>II', texture[16:24]) == dimensions
assert archive[75200202]['Light']['m_Shadows']['m_Type'] == 1
assert archive[75200202]['Light']['m_Type'] == 0
assert archive_component(75200201)['m_AdditionalLightsShadowResolutionTier'] == 0
print('Workroom map floors, reserve locations, difficulty limits, PNG assets, breath and basement spotlight passed.')

print('Archive floor ring, stair geometry, safe workroom, phone, outdoor pickup point and camera links passed.')


areas = yaml.safe_load((root / 'ProjectSettings/NavMeshAreas.asset').read_text().split('\n', 3)[3])['NavMeshProjectSettings']['areas']
assert areas[3]['name'] == 'Salt' and areas[4]['name'] == 'Hiding'
input_data = json.loads((assets / 'Input/PlayerControls.inputactions').read_text())
player_map = next(item for item in input_data['maps'] if item['name'] == 'Player')
for name, path in [('Drop', '<Keyboard>/g'), ('Interact', '<Keyboard>/e'), ('Use', '<Keyboard>/f'), ('Slot1', '<Keyboard>/1'), ('Slot2', '<Keyboard>/2'), ('Slot3', '<Keyboard>/3')]:
    assert any(binding['action'] == name and binding['path'] == path for binding in player_map['bindings']), (name, path)
for path in (assets / 'Editor').glob('*.cs'):
    for reference in re.findall(r'LoadAssetAtPath<\w+>\("([^"]+)"\)', path.read_text()):
        assert (root / reference).exists(), f'Missing validation asset: {reference}'
assert not (assets / 'Editor/SoundHidingValidation.cs').exists(), 'Removed fixture must stay removed.'
audio_profile = yaml.safe_load((assets / 'Settings/PrototypeAudio.asset').read_text().split('\n', 3)[3])['MonoBehaviour']
library_bindings = {
    'ghostLoop': 'HuntWail', 'doorOpen': 'WoodenDoorCreak', 'itemThrow': 'ItemThrowSweep',
    'impact': 'ItemContact', 'heavyImpact': 'ItemHeavyContact', 'cabinetClose': 'CabinetLatch',
    'flashlightToggle': 'FlashlightToggle', 'mechanicalSwitch': 'MechanicalSwitch',
    'writingScratch': 'WritingScratch', 'mapPageClick': 'MapPageClick',
    'inventoryClick': 'InventoryClick', 'cameraTune': 'CameraTune',
    'monitorShutdown': 'MonitorShutdown', 'monitorFan': 'MonitorFan', 'carPass': 'CompanyCarPass',
    'extractionConfirm': 'ExtractionConfirm', 'ghostAttack': 'GhostAttackGrowl',
}
loops = {'ghostLoop', 'monitorFan'}
for field in ['playerStep', 'ghostStep', 'huntStart', 'huntEnd', 'doorClose', *library_bindings]:
    reference = audio_profile[field]
    assert reference['fileID'] == 8300000 and reference['guid'] in guids, f'Missing clip: {field}'
    clip = Path(str(guids[reference['guid']])[:-5])
    if field in library_bindings:
        assert clip == assets / 'Audio/ArchiveSoundLibrary' / f'{library_bindings[field]}.wav'
    with wave.open(str(clip), 'rb') as stream:
        assert (stream.getnchannels(), stream.getsampwidth(), stream.getframerate()) == (1, 2, 24000)
        pcm = stream.readframes(stream.getnframes())
    samples = struct.unpack('<' + 'h' * (len(pcm) // 2), pcm)
    assert 0 < max(abs(value) for value in samples) < 32767, f'Empty/clipped clip: {clip}'
    if field in loops:
        assert abs(samples[0] - samples[-1]) < 1311, f'Loop seam jump: {clip}'
    else:
        assert samples[0] == 0 and (field not in library_bindings or samples[-1] == 0), f'One-shot fade missing: {clip}'
assert len(list((assets / 'Audio/ArchiveSoundLibrary').glob('*.wav'))) == len(library_bindings)
assert sum(path.stat().st_size for path in (assets / 'Audio/ArchiveSoundLibrary').glob('*.wav')) < 2 * 1024 * 1024
assert sum(path.stat().st_size for path in (assets / 'Audio').glob('*.wav') if path.name != 'ArchiveDispatchReply.wav') < 640 * 1024
sequence = yaml.safe_load((assets / 'Settings/HuntSequence.asset').read_text().split('\n', 3)[3])['MonoBehaviour']
assert sequence['warningDuration'] >= 0 and sequence['flickerInterval'] >= 0.1
print('Shared audio and 17 library cues passed PCM, reference, fades and loop-seam checks.')
print(f'{len(sources)} C# files parsed; {len(guids)} unique GUIDs; input, area IDs and validation asset paths passed.')
print('Static checks only. Unity compilation, Editor validations, physics, NavMesh and gameplay are NOT run by this script.')
