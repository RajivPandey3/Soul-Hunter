"""Run an explicitly supplied command with bounded time and immutable evidence."""
import argparse
import datetime
import json
from pathlib import Path
import subprocess

parser = argparse.ArgumentParser()
parser.add_argument('--output', required=True)
parser.add_argument('--timeout', type=int, default=120)
parser.add_argument('command', nargs=argparse.REMAINDER)
args = parser.parse_args()
command = args.command[1:] if args.command[:1] == ['--'] else args.command
if not command or args.timeout <= 0:
    parser.error('A command and positive timeout are required')
out = Path(args.output).resolve()
out.mkdir(parents=True, exist_ok=False)
record = {'command': command, 'cwd': str(Path.cwd()), 'timeoutSeconds': args.timeout,
          'startedUtc': datetime.datetime.now(datetime.timezone.utc).isoformat()}
with (out / 'stdout.txt').open('wb') as stdout, (out / 'stderr.txt').open('wb') as stderr:
    try:
        process = subprocess.Popen(command, stdout=stdout, stderr=stderr)
        record['pid'] = process.pid
        try:
            record['exitCode'] = process.wait(timeout=args.timeout)
            record['timedOut'] = False
        except subprocess.TimeoutExpired:
            # Kill only this launched process tree, never existing Editors.
            subprocess.run(['taskkill', '/PID', str(process.pid), '/T', '/F'],
                           stdout=stderr, stderr=stderr, timeout=15)
            process.wait(timeout=15)
            record.update(exitCode=process.returncode, timedOut=True)
    except (OSError, subprocess.SubprocessError) as error:
        record['captureError'] = str(error)
record['finishedUtc'] = datetime.datetime.now(datetime.timezone.utc).isoformat()
(out / 'result.json').write_text(json.dumps(record, indent=2), encoding='utf-8')
print(json.dumps(record))
raise SystemExit(124 if record.get('timedOut') else record.get('exitCode', 125))
