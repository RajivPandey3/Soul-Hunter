"""Read-only inventory/integrity evidence for TASK-001; never runs gameplay."""
from pathlib import Path
import datetime
import hashlib
import json
import re
import subprocess

out = Path('Reports/Validation/TASK-001')
baseline = json.loads((out / 'baseline.json').read_text())
changed = []
for name, digest in baseline['files'].items():
    path = Path(name)
    if not path.is_file() or hashlib.sha256(path.read_bytes()).hexdigest() != digest:
        changed.append(name)
bootstrap = ['AGENTS.md'] + [p.as_posix() for p in Path('CTO').glob('*.md')]
bootstrap += ['Reports/Reviews/INITIAL-AUDIT.md']
bootstrap += [p.as_posix() for root in ['Docs', 'Tasks', 'Tests', 'Reports', 'Tools']
              for p in Path(root).rglob('.gitkeep')]
tests = [p.as_posix() for root in ['Assets/Tests', 'Assets/_Project/Tests', 'Tests']
         for p in Path(root).rglob('*') if p.is_file()]
annotated = [p.as_posix() for p in Path('Assets').rglob('*.cs')
             if re.search(r'\[(?:Test|UnityTest)(?:\]|\()', p.read_text(errors='replace'))]
bible = Path('Assets/_Project/Gameplay/Weapons/BibleWeapon.cs')
log = Path.home() / 'AppData/Local/Unity/Editor/Editor.log'
lines = log.read_text(errors='replace').splitlines() if log.exists() else []
indices = [i for i, line in enumerate(lines) if 'Adding component failed.' in line]
excerpt = []
for i in indices[-2:]:
    excerpt.extend(lines[i:i+85])
(out / 'bible-existing-stack.txt').write_text('\n'.join(excerpt), encoding='utf-8')
record = {
    'timestampUtc': datetime.datetime.now(datetime.timezone.utc).isoformat(),
    'changedBaselineFiles': changed,
    'bootstrapFiles': sorted(bootstrap),
    'testDirectoryFiles': tests,
    'unityTestAnnotatedSources': annotated,
    'regressionTestHashUnchanged': hashlib.sha256(Path('Tools/ArchitectureChecks/Regression.cs').read_bytes()).hexdigest() == baseline['files']['Tools/ArchitectureChecks/Regression.cs'],
    'bibleHashUnchanged': hashlib.sha256(bible.read_bytes()).hexdigest() == baseline['files'][bible.as_posix()],
    'headUnchanged': subprocess.check_output(['git', 'rev-parse', 'HEAD'], text=True).strip() == baseline['head'],
    'stagedDiff': subprocess.check_output(['git', 'diff', '--cached', '--stat'], text=True),
}
(out / 'integrity.json').write_text(json.dumps(record, indent=2), encoding='utf-8')
(out / 'git-after.txt').write_text(subprocess.check_output(['git', 'status', '--porcelain=v1', '-uall'], text=True), encoding='utf-8')
print(json.dumps(record, indent=2))
