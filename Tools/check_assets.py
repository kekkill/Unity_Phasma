"""Static Unity asset checks only; does not compile C# or execute Unity physics/navigation.
Run: PYTHONPATH=/tmp/phasma-syntax-tools python Tools/check_assets.py
"""
from pathlib import Path
import json
import re
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
    components = [obj['MonoBehaviour'] for obj in objects.values() if 'MonoBehaviour' in obj]
    for pickup in [data for data in components if data['m_Script']['guid'] == pickup_guid]:
        assert any(data['m_GameObject'] == pickup['m_GameObject'] and data['m_Script']['guid'] == noise_guid for data in components)
    if scene == 'RoomSearchPrototype':
        zone = mono(65000003)
        assert zone['door']['fileID'] == 65000102 and zone['approachPoint']['fileID'] == 65000051
        assert objects[65000002]['BoxCollider']['m_IsTrigger'] == 1
        nav = mono(1971981892)
        assert {65000014, 65000024, 65000034, 65000044}.issubset({ref['fileID'] for ref in nav['geometry']})
        assert {1556033313, 65000114} == {ref['fileID'] for ref in nav['movingObstacles']}
        assert mono(65000102)['initiallyOpen'] == 0 and mono(65000102)['openAngle'] == -90
        # Closet root/body remain inside the Bedroom's existing six-metre footprint.
        position = objects[65000001]['Transform']['m_LocalPosition']
        assert -8 < position['x'] - 1.075 and position['x'] + 1.075 < -2
        assert -3 < position['z'] - 1.075 and position['z'] + 1.075 < 3
    print(f'{scene}: {len(objects)} unique objects; local references, owners and sound/hiding wiring passed.')

areas = yaml.safe_load((root / 'ProjectSettings/NavMeshAreas.asset').read_text().split('\n', 3)[3])['NavMeshProjectSettings']['areas']
assert areas[3]['name'] == 'Salt' and areas[4]['name'] == 'Hiding'
input_data = json.loads((assets / 'Input/PlayerControls.inputactions').read_text())
player_map = next(item for item in input_data['maps'] if item['name'] == 'Player')
for name, path in [('Drop', '<Keyboard>/g'), ('Interact', '<Keyboard>/e'), ('Use', '<Keyboard>/f'), ('Slot1', '<Keyboard>/1'), ('Slot2', '<Keyboard>/2'), ('Slot3', '<Keyboard>/3')]:
    assert any(binding['action'] == name and binding['path'] == path for binding in player_map['bindings']), (name, path)
for path in (assets / 'Editor').glob('*.cs'):
    for reference in re.findall(r'LoadAssetAtPath<\w+>\("([^"]+)"\)', path.read_text()):
        assert (root / reference).exists(), f'Missing validation asset: {reference}'
print(f'{len(sources)} C# files parsed; {len(guids)} unique GUIDs; input, area IDs and validation asset paths passed.')
print('Static checks only. Unity compilation, Editor validations, physics, NavMesh and gameplay are NOT run by this script.')
