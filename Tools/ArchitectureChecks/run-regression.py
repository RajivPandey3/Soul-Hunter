from pathlib import Path
import subprocess, json, shutil, tempfile, os
root = Path(__file__).resolve().parents[2]
src = root / 'Assets/_Project'
test = Path(__file__).with_name('Regression.cs')
dotnet = os.environ.get('DOTNET_EXE') or shutil.which('dotnet') or 'D:/dotnet/dotnet.exe'
sdk_lines = subprocess.check_output([dotnet, '--list-sdks'], text=True).splitlines()
sdk_line = next((line for line in reversed(sdk_lines) if line.startswith('8.')), None)
if sdk_line is None: raise SystemExit('Install .NET 8 SDK or set DOTNET_EXE to a .NET installation containing it.')
version, sdk_directory = sdk_line.split(' [', 1)
sdk = Path(sdk_directory.rstrip(']')) / version
ref = sorted((sdk.parent.parent / 'packs/Microsoft.NETCore.App.Ref').glob('8.*/ref/net8.0'))[-1]
files = [test] + [src / name for name in [
    'Tools/Validation/ProjectValidatorLayer2.cs',
    'Core/Services/GameServices.cs', 'Core/Services/IGameService.cs',
    'Core/Events/EventBus.cs', 'Core/Events/GameEvent.cs',
    'Core/Events/EventSubscription.cs', 'Core/Events/EventRegistry.cs',
    'Core/Events/EventSubscriptionEntry.cs',
    'Save/SaveData/SaveService.cs', 'Save/SaveData/GameData.cs',
    'Core/Services/SceneService.cs']]
missing = [str(path) for path in files if not path.is_file()]
if missing:
    raise SystemExit('Required regression sources missing:\n' + '\n'.join(missing))
out = Path(tempfile.mkdtemp(prefix='soul-hunter-regression-'))
args = ['-nologo', '-target:exe', '-langversion:9.0', '-out:"' + str(out/'Regression.dll') + '"']
args += ['-r:"' + str(p) + '"' for p in ref.glob('*.dll')]
args += ['"' + str(p) + '"' for p in files]
rsp = out/'Regression.rsp'; rsp.write_text('\n'.join(args))
subprocess.run([dotnet, str(sdk/'Roslyn/bincore/csc.dll'), '@'+str(rsp)], check=True)
(out/'Regression.runtimeconfig.json').write_text(json.dumps({'runtimeOptions': {'tfm': 'net8.0', 'framework': {'name': 'Microsoft.NETCore.App', 'version': '8.0.0'}}}))
result = subprocess.run([dotnet, str(out/'Regression.dll')], capture_output=True, text=True)
(out/'results.log').write_text(result.stdout + result.stderr)
print(result.stdout + result.stderr)
print('Test artifacts:', out)
raise SystemExit(result.returncode)
