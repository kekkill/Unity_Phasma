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
guids = {}
for path in assets.rglob('*.meta'):
    match = re.search(r'^guid: (\w+)', path.read_text(), re.M)
    assert match, f'Missing GUID: {path}'
    assert match[1] not in guids, f'Duplicate GUID: {path}, {guids.get(match[1])}'
    guids[match[1]] = path

def scene_objects(path):
    text = path.read_text()
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

for scene in ['Prototype', 'RoomSearchPrototype']:
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
assert catalog['sceneName'] == 'RoomSearchPrototype'
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
assert build[0]['path'] == 'Assets/Scenes/MainMenu.unity' and build[0]['enabled'] == 1
for scene in build:
    if not scene['enabled']: continue
    assert (root / scene['path']).exists()
    assert guids[scene['guid']] == root / (scene['path'] + '.meta')
print(f'MainMenu: {len(menu)} objects; build order, difficulty/catalog, equipment and power references passed.')

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
for field in ['playerStep', 'ghostStep', 'ghostLoop', 'huntStart', 'huntEnd', 'doorOpen', 'doorClose', 'itemThrow', 'impact']:
    reference = audio_profile[field]
    assert reference['fileID'] == 8300000 and reference['guid'] in guids, f'Missing clip: {field}'
    clip = Path(str(guids[reference['guid']])[:-5])
    with wave.open(str(clip), 'rb') as stream:
        assert (stream.getnchannels(), stream.getsampwidth(), stream.getframerate()) == (1, 2, 24000)
        pcm = stream.readframes(stream.getnframes())
    samples = struct.unpack('<' + 'h' * (len(pcm) // 2), pcm)
    assert 0 < max(abs(value) for value in samples) < 32767 and samples[0] == 0, f'Empty/clipped clip: {clip}'
assert sum(path.stat().st_size for path in (assets / 'Audio').glob('*.wav')) < 512 * 1024
sequence = yaml.safe_load((assets / 'Settings/HuntSequence.asset').read_text().split('\n', 3)[3])['MonoBehaviour']
assert sequence['warningDuration'] >= 0 and sequence['flickerInterval'] >= 0.1
print('Hunt entrance/lighting/audio links and nine bounded mono PCM clips passed.')
print(f'{len(sources)} C# files parsed; {len(guids)} unique GUIDs; input, area IDs and validation asset paths passed.')
print('Static checks only. Unity compilation, Editor validations, physics, NavMesh and gameplay are NOT run by this script.')
