import json, re, sys
data = json.load(open(sys.argv[1]))
s = json.dumps(data, ensure_ascii=False, indent=1)
num = r'-?\d+(?:\.\d+)?(?:[eE][+-]?\d+)?'
s = re.sub(r'\[\s*(' + num + r'(?:,\s*' + num + r')*)\s*\]', lambda m: '[' + re.sub(r'\s+', '', m.group(1)).replace(',', ', ') + ']', s)
open(sys.argv[2], 'w').write(s + '\n')
