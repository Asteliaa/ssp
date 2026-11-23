"""Текст контрактов, написанных в работе, без комментариев и пустых строк (для приложения)."""
import re
from pathlib import Path
SOL = Path(r'D:\Unik\SSP\Лаб7_СмартКонтракт')

def student_code():
    lines = SOL.read_text(encoding='utf-8').split('\n')
    start = next(i for i, l in enumerate(lines) if l.startswith('contract BookRegistry'))
    out = []
    for l in lines[start:]:
        s = l.rstrip()
        st = s.strip()
        if not st or st.startswith('//') or st.startswith('/*') or st.startswith('*'):
            continue
        s = re.sub(r'\s+//.*$', '', s)
        s = re.sub(r'(?<=\S)  +', ' ', s)
        out.append(s)
    return out

if __name__ == '__main__':
    c = student_code()
    print(len(c), 'lines; >=60:', sum(len(x) >= 60 for x in c), 'max', max(map(len, c)))
    for x in c:
        if len(x) >= 60: print(len(x), x)


BREAKS = (', ', ') ', ' + ', ' && ', ' || ', ' public ', ' external ', ' internal ', ' returns ', ' onlyAdmin ', ' == ', ' = ')


def wrap(line, limit=59):
    """Перенос длинной строки кода по границе выражения с отступом продолжения."""
    if len(line) <= limit:
        return [line]
    ind = len(line) - len(line.lstrip())
    best = None
    for b in BREAKS:
        k = line.find(b, ind + 8)
        while k != -1:
            keep = {', ': 1, ') ': 1, ' + ': 2, ' && ': 3, ' || ': 3, ' = ': 2}.get(b, 0)
            cut = k + keep
            if cut <= limit and (best is None or cut > best):
                best = cut
            k = line.find(b, k + 1)
    if best is None:
        return [line]
    head = line[:best].rstrip()
    tail = ' ' * (ind + 4) + line[best:].lstrip()
    return [head] + wrap(tail, limit)


def student_code_wrapped():
    out = []
    for l in student_code():
        out.extend(wrap(l))
    return out


def contracts(names):
    """Код указанных контрактов без комментариев и пустых строк."""
    lines = []
    for f in ('StudentRegistry.sol', 'BookRegistry.sol', 'EmployeeRegistry.sol'):
        lines += (SOL / f).read_text(encoding='utf-8').split('\n')
    out, take = [], False
    for l in lines:
        m = re.match(r'contract (\w+)', l)
        if m:
            take = m.group(1) in names
        if not take:
            continue
        s = l.rstrip()
        st = s.strip()
        if not st or st.startswith(('//', '/*', '*')):
            continue
        s = re.sub(r'\s+//.*$', '', s)
        s = re.sub(r'(?<=\S)  +', ' ', s)
        out.append(s)
        if s == '}':
            take = False
    return out


HEADS = ('function ', 'constructor', 'event ', 'modifier ')


def join_headers(lines, limit=84):
    """Многострочное объявление функции собирается и переносится по ширине строки."""
    out, i = [], 0
    while i < len(lines):
        cur = lines[i]
        st = cur.strip()
        if st.startswith(HEADS) and not st.endswith(('{', ';')):
            j, acc = i + 1, cur
            while j < len(lines):
                nxt = lines[j].strip()
                sep = '' if acc.endswith('(') or nxt.startswith(')') else ' '
                acc, j = acc + sep + nxt, j + 1
                if nxt.endswith(('{', ';')):
                    break
            out.extend(wrap(acc, limit))
            i = j
            continue
        out.extend(wrap(cur, limit))
        i += 1
    return out
