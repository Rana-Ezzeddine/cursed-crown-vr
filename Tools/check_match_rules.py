#!/usr/bin/env python3
"""Compile and execute pure C# rule checks; does not claim Unity/NGO verification."""
import json
import os
from pathlib import Path
import subprocess
import tempfile

root = Path(__file__).resolve().parents[1]
sdk = Path(os.environ.get('UNITY_DOTNET_SDK', '/Applications/Unity/Hub/Editor/6000.6.2f1/Unity.app/Contents/Resources/Scripting/DotNetSdk'))
with tempfile.TemporaryDirectory(prefix='cursed-crown-rules-') as temporary:
    output = Path(temporary)
    csc = next((sdk / 'sdk').glob('*/Roslyn/bincore/csc.dll'))
    reference = next((sdk / 'packs/Microsoft.NETCore.App.Ref').glob('*/ref/net8.0'))
    runtime_version = next((sdk / 'shared/Microsoft.NETCore.App').iterdir()).name
    for name in ('MatchClockChecks', 'CrownMatchChecks'):
        binary = output / (name + '.dll')
        sources = [root / 'Assets/CursedCrown/Core/GameRules.cs', root / 'Assets/CursedCrown/Match/MatchClock.cs', root / 'Assets/CursedCrown/Match/CrownMatchRules.cs', root / ('Tools/' + name + '.cs')]
        args = ['-nologo', '-target:exe', '-out:' + str(binary)] + ['-r:' + str(p) for p in reference.glob('*.dll')] + list(map(str, sources))
        response = output / (name + '.rsp')
        response.write_text('\n'.join('"' + arg + '"' for arg in args))
        subprocess.run([str(sdk / 'dotnet'), str(csc), '@' + str(response)], check=True)
        binary.with_suffix('.runtimeconfig.json').write_text(json.dumps({'runtimeOptions': {'tfm': 'net8.0', 'framework': {'name': 'Microsoft.NETCore.App', 'version': runtime_version}}}))
        subprocess.run([str(sdk / 'dotnet'), str(binary)], check=True)
